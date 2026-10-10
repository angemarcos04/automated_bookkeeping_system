<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Account;
use App\Models\CashDisbursement;
use App\Models\JournalEntry;
use App\Services\JournalEntryService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\ValidationException;

class CashDisbursementController extends Controller
{
    public function index(): JsonResponse
    {
        return response()->json([
            'success' => true,
            'data' => CashDisbursement::with(['account', 'paymentAccount'])
                ->orderByDesc('disbursement_date')
                ->get(),
        ]);
    }

    public function store(
        Request $request,
        JournalEntryService $journalEntryService
    ): JsonResponse {
        $validated = $request->validate([
            'disbursement_date' => ['required', 'date'],
            'voucher_number' => [
                'required',
                'string',
                'max:30',
                'unique:cash_disbursements,voucher_number',
            ],
            'payee' => ['required', 'string', 'max:150'],
            'account_id' => ['required', 'integer', 'exists:accounts,id'],
            'payment_account_id' => [
                'required',
                'integer',
                'different:account_id',
                'exists:accounts,id',
            ],
            'amount' => ['required', 'numeric', 'min:0.01'],
            'payment_method' => [
                'required',
                'string',
                'in:Cash,Check,Bank Transfer',
            ],
            'check_number' => ['nullable', 'string', 'max:50'],
            'remarks' => ['nullable', 'string'],
        ]);

        $expenseAccount = Account::query()
            ->whereKey($validated['account_id'])
            ->where('is_active', true)
            ->first();

        $paymentAccount = Account::query()
            ->whereKey($validated['payment_account_id'])
            ->where('is_active', true)
            ->first();

        if (! $expenseAccount || ! $paymentAccount) {
            throw ValidationException::withMessages([
                'account_id' => 'Both accounts must exist and be active.',
            ]);
        }

        if (! in_array(
            $paymentAccount->account_type,
            ['Asset'],
            true
        )) {
            throw ValidationException::withMessages([
                'payment_account_id' =>
                    'The payment account must be an asset account, such as cash or bank.',
            ]);
        }

        if ($expenseAccount->id === $paymentAccount->id) {
            throw ValidationException::withMessages([
                'payment_account_id' =>
                    'The account being debited and payment account must be different.',
            ]);
        }

        $disbursement = DB::transaction(function () use (
            $validated,
            $journalEntryService
        ) {
            $disbursement = CashDisbursement::create($validated);

            $entry = JournalEntry::create([
                'journal_number' => 'CD-' . $disbursement->id,
                'journal_date' => $disbursement->disbursement_date,
                'description' => 'Cash disbursement to '
                    . $disbursement->payee
                    . ' (Voucher '
                    . $disbursement->voucher_number
                    . ')',
                'reference_type' => 'CashDisbursement',
                'reference_id' => $disbursement->id,
                'status' => 'Draft',
                'created_by' => auth()->id(),
            ]);

            $entry->lines()->createMany([
                [
                    'account_id' => $disbursement->account_id,
                    'description' => 'Disbursement expense/payable',
                    'debit_amount' => $disbursement->amount,
                    'credit_amount' => 0,
                ],
                [
                    'account_id' => $disbursement->payment_account_id,
                    'description' => 'Cash/bank payment',
                    'debit_amount' => 0,
                    'credit_amount' => $disbursement->amount,
                ],
            ]);

            $journalEntryService->post($entry);

            return $disbursement;
        });

        return response()->json([
            'success' => true,
            'message' =>
                'Cash disbursement created and journal entry posted successfully.',
            'data' => $disbursement->load([
                'account',
                'paymentAccount',
            ]),
        ], 201);
    }

    public function show(
        CashDisbursement $cashDisbursement
    ): JsonResponse {
        return response()->json([
            'success' => true,
            'data' => $cashDisbursement->load([
                'account',
                'paymentAccount',
            ]),
        ]);
    }

    public function update(
        Request $request,
        CashDisbursement $cashDisbursement
    ): JsonResponse {
        $hasJournalEntry = JournalEntry::query()
            ->where('reference_type', 'CashDisbursement')
            ->where('reference_id', $cashDisbursement->id)
            ->exists();

        if ($hasJournalEntry) {
            throw ValidationException::withMessages([
                'disbursement' =>
                    'This disbursement has an accounting entry and cannot be edited. Use a correcting journal entry instead.',
            ]);
        }

        $validated = $request->validate([
            'disbursement_date' => ['required', 'date'],
            'voucher_number' => [
                'required',
                'string',
                'max:30',
                'unique:cash_disbursements,voucher_number,'
                    . $cashDisbursement->id,
            ],
            'payee' => ['required', 'string', 'max:150'],
            'account_id' => ['required', 'integer', 'exists:accounts,id'],
            'payment_account_id' => [
                'required',
                'integer',
                'different:account_id',
                'exists:accounts,id',
            ],
            'amount' => ['required', 'numeric', 'min:0.01'],
            'payment_method' => [
                'required',
                'string',
                'in:Cash,Check,Bank Transfer',
            ],
            'check_number' => ['nullable', 'string', 'max:50'],
            'remarks' => ['nullable', 'string'],
        ]);

        $cashDisbursement->update($validated);

        return response()->json([
            'success' => true,
            'message' => 'Cash disbursement updated successfully.',
            'data' => $cashDisbursement->load([
                'account',
                'paymentAccount',
            ]),
        ]);
    }

    public function destroy(
        CashDisbursement $cashDisbursement
    ): JsonResponse {
        $hasJournalEntry = JournalEntry::query()
            ->where('reference_type', 'CashDisbursement')
            ->where('reference_id', $cashDisbursement->id)
            ->exists();

        if ($hasJournalEntry) {
            throw ValidationException::withMessages([
                'disbursement' =>
                    'This disbursement has an accounting entry and cannot be deleted.',
            ]);
        }

        $cashDisbursement->delete();

        return response()->json([
            'success' => true,
            'message' => 'Cash disbursement deleted successfully.',
        ]);
    }
}