<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Account;
use Illuminate\Http\JsonResponse;
use Illuminate\Support\Facades\DB;

class TrialBalanceController extends Controller
{
    public function index(): JsonResponse
    {
        $accounts = Account::query()
            ->where('is_active', true)
            ->orderBy('account_code')
            ->get();

        $balances = DB::table('journal_entry_lines as lines')
            ->join(
                'journal_entries as entries',
                'entries.id',
                '=',
                'lines.journal_entry_id'
            )
            ->where('entries.status', 'Posted')
            ->select(
                'lines.account_id',
                DB::raw('SUM(lines.debit_amount) as total_debits'),
                DB::raw('SUM(lines.credit_amount) as total_credits')
            )
            ->groupBy('lines.account_id')
            ->get()
            ->keyBy('account_id');

        $totalDebits = 0.0;
        $totalCredits = 0.0;
        $rows = [];

        foreach ($accounts as $account) {
            $balance = $balances->get($account->id);

            $debits = (float) ($balance->total_debits ?? 0);
            $credits = (float) ($balance->total_credits ?? 0);

            $netBalance = $debits - $credits;

            if ($netBalance >= 0) {
                $debitBalance = $netBalance;
                $creditBalance = 0;
            } else {
                $debitBalance = 0;
                $creditBalance = abs($netBalance);
            }

            $totalDebits += $debitBalance;
            $totalCredits += $creditBalance;

            $rows[] = [
                'account_id' => $account->id,
                'account_code' => $account->account_code,
                'account_name' => $account->account_name,
                'account_type' => $account->account_type,
                'debit_balance' => number_format(
                    $debitBalance,
                    2,
                    '.',
                    ''
                ),
                'credit_balance' => number_format(
                    $creditBalance,
                    2,
                    '.',
                    ''
                ),
            ];
        }

        return response()->json([
            'success' => true,
            'data' => $rows,
            'totals' => [
                'debits' => number_format($totalDebits, 2, '.', ''),
                'credits' => number_format($totalCredits, 2, '.', ''),
                'is_balanced' => abs(
                    $totalDebits - $totalCredits
                ) < 0.005,
            ],
        ]);
    }
}