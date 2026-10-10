<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;

class DashboardController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'start_date' => ['nullable', 'date_format:Y-m-d'],
            'end_date' => [
                'nullable',
                'date_format:Y-m-d',
                'after_or_equal:start_date',
            ],
        ]);

        $startDate = $validated['start_date'] ?? null;
        $endDate = $validated['end_date'] ?? null;

        // Balance-sheet figures include all posted entries
        // up to the selected end date.
        $balancesQuery = DB::table('journal_entry_lines')
            ->join(
                'journal_entries',
                'journal_entry_lines.journal_entry_id',
                '=',
                'journal_entries.id'
            )
            ->join(
                'accounts',
                'journal_entry_lines.account_id',
                '=',
                'accounts.id'
            )
            ->where('journal_entries.status', 'Posted');

        if ($endDate) {
            $balancesQuery->whereDate(
                'journal_entries.journal_date',
                '<=',
                $endDate
            );
        }

        $balances = $balancesQuery
            ->select(
                'accounts.id',
                'accounts.account_name',
                'accounts.account_type',
                DB::raw('SUM(journal_entry_lines.debit_amount) as total_debits'),
                DB::raw('SUM(journal_entry_lines.credit_amount) as total_credits')
            )
            ->groupBy(
                'accounts.id',
                'accounts.account_name',
                'accounts.account_type'
            )
            ->get();

        $cashBalance = 0;
        $receivables = 0;
        $payables = 0;
        $assets = 0;
        $liabilities = 0;
        $equity = 0;

        foreach ($balances as $account) {
            $netDebit =
                (float) $account->total_debits
                - (float) $account->total_credits;

            $name = strtolower($account->account_name);
            $type = strtolower($account->account_type);

            if ($type === 'asset') {
                $assets += $netDebit;

                if (str_starts_with($name, 'cash')) {
                    $cashBalance += $netDebit;
                }

                if (str_contains($name, 'accounts receivable')) {
                    $receivables += $netDebit;
                }
            }

            if ($type === 'liability') {
                $liabilities -= $netDebit;
                $payables -= $netDebit;
            }

            if ($type === 'equity') {
                $equity -= $netDebit;
            }
        }

        // Income and expenses cover only the selected period.
        $periodQuery = DB::table('journal_entry_lines')
            ->join(
                'journal_entries',
                'journal_entry_lines.journal_entry_id',
                '=',
                'journal_entries.id'
            )
            ->join(
                'accounts',
                'journal_entry_lines.account_id',
                '=',
                'accounts.id'
            )
            ->where('journal_entries.status', 'Posted');

        if ($startDate) {
            $periodQuery->whereDate(
                'journal_entries.journal_date',
                '>=',
                $startDate
            );
        }

        if ($endDate) {
            $periodQuery->whereDate(
                'journal_entries.journal_date',
                '<=',
                $endDate
            );
        }

        $periodTotals = $periodQuery
            ->select(
                'accounts.account_type',
                DB::raw('SUM(journal_entry_lines.debit_amount) as total_debits'),
                DB::raw('SUM(journal_entry_lines.credit_amount) as total_credits')
            )
            ->groupBy('accounts.account_type')
            ->get();

        $income = 0;
        $expenses = 0;

        foreach ($periodTotals as $account) {
            $type = strtolower($account->account_type);
            $debits = (float) $account->total_debits;
            $credits = (float) $account->total_credits;

            if ($type === 'revenue') {
                $income += $credits - $debits;
            }

            if ($type === 'expense') {
                $expenses += $debits - $credits;
            }
        }

        return response()->json([
            'success' => true,
            'filters' => [
                'start_date' => $startDate,
                'end_date' => $endDate,
            ],
            'data' => [
                'cash_balance' => round($cashBalance, 2),
                'income' => round($income, 2),
                'expenses' => round($expenses, 2),
                'receivables' => round($receivables, 2),
                'payables' => round($payables, 2),
                'assets' => round($assets, 2),
                'financial_summary' => [
                    'total_assets' => round($assets, 2),
                    'total_liabilities' => round($liabilities, 2),
                    'total_equity' => round($equity, 2),
                    'total_income' => round($income, 2),
                    'total_expenses' => round($expenses, 2),
                    'net_income' => round($income - $expenses, 2),
                ],
            ],
        ]);
    }
}