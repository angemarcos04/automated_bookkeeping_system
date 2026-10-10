<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\FixedAsset;
use Carbon\Carbon;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class LapsingScheduleController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'as_of' => ['sometimes', 'date'],
            'date' => ['sometimes', 'date'],
            'period' => ['sometimes', 'in:daily,weekly,monthly,yearly'],
        ]);

        $period = $validated['period'] ?? 'daily';

        $referenceDate = Carbon::parse(
            $validated['date']
                ?? $validated['as_of']
                ?? now()->toDateString()
        )->startOfDay();

        switch ($period) {
            case 'weekly':
                $startDate = $referenceDate->copy()->startOfWeek(
                    Carbon::MONDAY
                );
                $endDate = $startDate->copy()->endOfWeek(
                    Carbon::SUNDAY
                );
                break;

            case 'monthly':
                $startDate = $referenceDate->copy()->startOfMonth();
                $endDate = $referenceDate->copy()->endOfMonth();
                break;

            case 'yearly':
                $startDate = $referenceDate->copy()->startOfYear();
                $endDate = $referenceDate->copy()->endOfYear();
                break;

            default:
                $startDate = $referenceDate->copy()->startOfDay();
                $endDate = $referenceDate->copy()->endOfDay();
                break;
        }

        // A lapsing schedule is a snapshot as of the selected period's
        // ending date. It is not a sum of separate daily snapshots.
        $asOf = $endDate->copy()->startOfDay();

        $assets = FixedAsset::where('is_active', true)
            ->orderBy('asset_code')
            ->get()
            ->map(function (FixedAsset $asset) use ($asOf) {
                $cost = (float) $asset->acquisition_cost;
                $salvage = (float) $asset->salvage_value;
                $life = (float) $asset->useful_life_years;

                $depreciableBase = max(0, $cost - $salvage);

                $annualDepreciation = $life > 0
                    ? $depreciableBase / $life
                    : 0;

                $yearsElapsed = 0.0;

                if ($asset->acquisition_date && $life > 0) {
                    $acquired = Carbon::parse(
                        $asset->acquisition_date
                    )->startOfDay();

                    if ($asOf->greaterThanOrEqualTo($acquired)) {
                        $yearsElapsed =
                            $acquired->diffInDays($asOf) / 365.2425;
                    }
                }

                $accumulatedDepreciation = min(
                    $depreciableBase,
                    max(0, $annualDepreciation * $yearsElapsed)
                );

                $netBookValue = max(
                    $salvage,
                    $cost - $accumulatedDepreciation
                );

                return [
                    'asset_code' => $asset->asset_code,
                    'name' => $asset->name,
                    'category' => $asset->category,
                    'acquisition_date' =>
                        $asset->acquisition_date?->toDateString(),
                    'acquisition_cost' => round($cost, 2),
                    'useful_life_years' => $life,
                    'salvage_value' => round($salvage, 2),
                    'annual_depreciation' =>
                        round($annualDepreciation, 2),
                    'accumulated_depreciation' =>
                        round($accumulatedDepreciation, 2),
                    'net_book_value' => round($netBookValue, 2),
                ];
            });

        return response()->json([
            'success' => true,
            'period' => $period,
            'period_start' => $startDate->toDateString(),
            'period_end' => $endDate->toDateString(),
            'as_of' => $asOf->toDateString(),
            'data' => $assets,
        ]);
    }
}