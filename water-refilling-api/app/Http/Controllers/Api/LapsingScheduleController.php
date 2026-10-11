<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\FixedAsset;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Carbon\Carbon;

class LapsingScheduleController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $request->validate([
            'as_of' => ['sometimes', 'date'],
        ]);

        $asOf = Carbon::parse(
            $request->query('as_of', now()->toDateString())
        )->startOfDay();


        $assets = FixedAsset::where('is_active', true)
            ->where(function ($query) use ($asOf) {
        $query->whereNull('acquisition_date')
            ->orWhereDate(
                'acquisition_date',
                '<=',
                $asOf->toDateString()
            );
    })
    ->orderBy('asset_code')
    ->get()


            ->map(function (FixedAsset $asset) use ($asOf) {
                $cost = (float) $asset->acquisition_cost;
                $salvage = (float) $asset->salvage_value;
                $life = (float) $asset->useful_life_years;

                $yearsElapsed = 0;

                if ($asset->acquisition_date) {
                    $acquired = Carbon::parse(
                        $asset->acquisition_date
                    )->startOfDay();

                    if ($asOf->greaterThanOrEqualTo($acquired)) {
                        $yearsElapsed =
                            $acquired->diffInDays($asOf) / 365.2425;
                    }
                }

                $depreciableBase = max(0, $cost - $salvage);

                $annualDepreciation = $life > 0
                    ? $depreciableBase / $life
                    : 0;

                $accumulatedDepreciation = min(
                    $depreciableBase,
                    $annualDepreciation * $yearsElapsed
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
            'as_of' => $asOf->toDateString(),
            'data' => $assets,
        ]);
    }
}