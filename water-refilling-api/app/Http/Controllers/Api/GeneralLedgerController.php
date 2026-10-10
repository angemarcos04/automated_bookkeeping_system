<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Account;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;

class GeneralLedgerController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'start_date' => ['nullable', 'date'],
            'end_date' => ['nullable', 'date', 'after_or_equal:start_date'],
            'account_id' => ['nullable', 'integer', 'exists:accounts,id'],
        ]);

        $query = Account::query()
            ->where('is_active', true)
            ->when(
                isset($validated['account_id']),
                fn ($query) => $query->where(
                    'id',
                    $validated['account_id']
                )
            )
            ->with([
                'journalEntryLines' => function ($query) use ($validated) {
                    $query->whereHas('journalEntry', function ($entryQuery) use ($validated) {
                        $entryQuery->where('status', 'Posted')
                            ->when(
                                isset($validated['start_date']),
                                fn ($q) => $q->whereDate(
                                    'journal_date',
                                    '>=',
                                    $validated['start_date']
                                )
                            )
                            ->when(
                                isset($validated['end_date']),
                                fn ($q) => $q->whereDate(
                                    'journal_date',
                                    '<=',
                                    $validated['end_date']
                                )
                            );
                    })->with('journalEntry')
                        ->orderBy('id');
                },
            ])
            ->orderBy('account_code');

        $accounts = $query->get()->map(function ($account) {
            $runningBalance = 0;

            $lines = $account->journalEntryLines->map(function ($line) use (
                &$runningBalance,
                $account
            ) {
                $debit = (float) $line->debit_amount;
                $credit = (float) $line->credit_amount;

                $normalDebitBalance = in_array(
                    $account->account_type,
                    ['Asset', 'Expense']
                );

                $runningBalance += $normalDebitBalance
                    ? $debit - $credit
                    : $credit - $debit;

                return [
                    'journal_entry_id' => $line->journal_entry_id,
                    'journal_number' => $line->journalEntry->journal_number,
                    'journal_date' => $line->journalEntry->journal_date,
                    'entry_description' => $line->journalEntry->description,
                    'line_description' => $line->description,
                    'debit' => number_format($debit, 2, '.', ''),
                    'credit' => number_format($credit, 2, '.', ''),
                    'running_balance' => number_format(
                        $runningBalance,
                        2,
                        '.',
                        ''
                    ),
                ];
            });

            return [
                'account_id' => $account->id,
                'account_code' => $account->account_code,
                'account_name' => $account->account_name,
                'account_type' => $account->account_type,
                'opening_balance' => '0.00',
                'total_debits' => number_format(
                    $account->journalEntryLines->sum(
                        fn ($line) => (float) $line->debit_amount
                    ),
                    2,
                    '.',
                    ''
                ),
                'total_credits' => number_format(
                    $account->journalEntryLines->sum(
                        fn ($line) => (float) $line->credit_amount
                    ),
                    2,
                    '.',
                    ''
                ),
                'closing_balance' => number_format(
                    $runningBalance,
                    2,
                    '.',
                    ''
                ),
                'entries' => $lines->values(),
            ];
        });

        return response()->json([
            'data' => $accounts->values(),
        ]);
    }
}