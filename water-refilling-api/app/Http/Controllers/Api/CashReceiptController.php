<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Account;
use App\Models\CashReceipt;
use App\Models\JournalEntry;
use App\Services\JournalEntryService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\ValidationException;

class CashReceiptController extends Controller
{
    public function index(): JsonResponse
    {
        return response()->json([
            'success' => true,
            'data' => CashReceipt::with([
                'account',
                'depositAccount',
            ])
                ->orderByDesc('receipt_date')
                ->get(),
        ]);
    }

    public function store(
        Request $request,
        JournalEntryService $journalEntryService
    ): JsonResponse {
        $validated = $request->validate([
            'receipt_date' => ['required', 'date'],
            'or_number' => [
                'required',
                'string',
                'max:30',
                'unique:cash_receipts,or_number',
            ],
            'payor' => ['required', 'string', 'max:150'],
            'account_id' => [
                'required',
                'integer',
                'exists:accounts,id',
            ],
            'deposit_account_id' => [
                'required',
                'integer',
                'exists:accounts,id',
            ],
            'amount' => ['required', 'numeric', 'min:0.01'],
            'bank_deposited' => ['nullable', 'string', 'max:150'],
            'remarks' => ['nullable', 'string'],
        ]);

        $cashReceipt = DB::transaction(function () use (
            $validated,
            $request,
            $journalEntryService
        ) {
            $depositAccount = Account::findOrFail(
                $validated['deposit_account_id']
            );

            $creditAccount = Account::findOrFail(
                $validated['account_id']
            );

            if (! $depositAccount->is_active) {
                throw ValidationException::withMessages([
                    'deposit_account_id' =>
                        'The selected deposit account is inactive.',
                ]);
            }

            if (! $creditAccount->is_active) {
                throw ValidationException::withMessages([
                    'account_id' =>
                        'The selected GL account is inactive.',
                ]);
            }

            if ($depositAccount->account_type !== 'Asset') {
                throw ValidationException::withMessages([
                    'deposit_account_id' =>
                        'The deposit account must be an Asset account.',
                ]);
            }

            if ($depositAccount->id === $creditAccount->id) {
                throw ValidationException::withMessages([
                    'account_id' =>
                        'The deposit account and GL account must be different.',
                ]);
            }

            $cashReceipt = CashReceipt::create($validated);

            $amount = number_format(
                (float) $validated['amount'],
                2,
                '.',
                ''
            );

            $entry = JournalEntry::create([
                'journal_number' => 'CR-' . $cashReceipt->id,
                'journal_date' => $validated['receipt_date'],
                'description' => 'Cash receipt OR '
                    . $validated['or_number']
                    . ' - '
                    . $validated['payor'],
                'reference_type' => 'CashReceipt',
                'reference_id' => $cashReceipt->id,
                'status' => 'Draft',
                'created_by' => $request->user()?->id,
            ]);

            $entry->lines()->createMany([
                [
                    'account_id' => $depositAccount->id,
                    'description' => 'Cash received - '
                        . $validated['payor'],
                    'debit_amount' => $amount,
                    'credit_amount' => '0.00',
                ],
                [
                    'account_id' => $creditAccount->id,
                    'description' => 'Receipt income/account - '
                        . $validated['payor'],
                    'debit_amount' => '0.00',
                    'credit_amount' => $amount,
                ],
            ]);

            $journalEntryService->post($entry);

            return $cashReceipt->load([
                'account',
                'depositAccount',
            ]);
        });

        return response()->json([
            'success' => true,
            'message' =>
                'Cash receipt created and journal entry posted successfully.',
            'data' => $cashReceipt,
        ], 201);
    }

    public function show(CashReceipt $cashReceipt): JsonResponse
    {
        return response()->json([
            'success' => true,
            'data' => $cashReceipt->load([
                'account',
                'depositAccount',
            ]),
        ]);
    }

    public function update(
        Request $request,
        CashReceipt $cashReceipt
    ): JsonResponse {
        $hasJournalEntry = JournalEntry::where(
            'reference_type',
            'CashReceipt'
        )
            ->where('reference_id', $cashReceipt->id)
            ->exists();

        if ($hasJournalEntry) {
            throw ValidationException::withMessages([
                'cash_receipt' =>
                    'This receipt has an accounting entry. '
                    . 'Editing it is disabled to preserve the audit trail.',
            ]);
        }

        $validated = $request->validate([
            'receipt_date' => ['required', 'date'],
            'or_number' => [
                'required',
                'string',
                'max:30',
                'unique:cash_receipts,or_number,' . $cashReceipt->id,
            ],
            'payor' => ['required', 'string', 'max:150'],
            'account_id' => [
                'required',
                'integer',
                'exists:accounts,id',
            ],
            'deposit_account_id' => [
                'required',
                'integer',
                'exists:accounts,id',
            ],
            'amount' => ['required', 'numeric', 'min:0.01'],
            'bank_deposited' => ['nullable', 'string', 'max:150'],
            'remarks' => ['nullable', 'string'],
        ]);

        $cashReceipt->update($validated);

        return response()->json([
            'success' => true,
            'message' => 'Cash receipt updated successfully.',
            'data' => $cashReceipt->load([
                'account',
                'depositAccount',
            ]),
        ]);
    }

    public function destroy(CashReceipt $cashReceipt): JsonResponse
    {
        $hasJournalEntry = JournalEntry::where(
            'reference_type',
            'CashReceipt'
        )
            ->where('reference_id', $cashReceipt->id)
            ->exists();

        if ($hasJournalEntry) {
            throw ValidationException::withMessages([
                'cash_receipt' =>
                    'This receipt has an accounting entry. '
                    . 'Deleting it is disabled to preserve the audit trail.',
            ]);
        }

        $cashReceipt->delete();

        return response()->json([
            'success' => true,
            'message' => 'Cash receipt deleted successfully.',
        ]);
    }
}