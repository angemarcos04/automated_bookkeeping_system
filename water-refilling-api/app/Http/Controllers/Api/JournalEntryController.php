<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Account;
use App\Models\JournalEntry;
use App\Services\JournalEntryService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\Rule;
use Illuminate\Validation\ValidationException;

class JournalEntryController extends Controller
{
    public function index(): JsonResponse
    {
        return response()->json(
            JournalEntry::with('lines.account')
                ->orderByDesc('journal_date')
                ->paginate(15)
        );
    }

    public function show(JournalEntry $journalEntry): JsonResponse
    {
        return response()->json(
            $journalEntry->load('lines.account', 'creator')
        );
    }

    public function store(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'journal_number' => ['required', 'string', 'max:50', 'unique:journal_entries,journal_number'],
            'journal_date' => ['required', 'date'],
            'description' => ['required', 'string', 'max:255'],
            'reference_type' => ['nullable', 'string', 'max:50'],
            'reference_id' => ['nullable', 'integer', 'min:1'],
            'lines' => ['required', 'array', 'min:2'],
            'lines.*.account_id' => ['required', 'integer', 'exists:accounts,id'],
            'lines.*.description' => ['nullable', 'string', 'max:255'],
            'lines.*.debit_amount' => ['required', 'numeric', 'min:0'],
            'lines.*.credit_amount' => ['required', 'numeric', 'min:0'],
        ]);

        $entry = DB::transaction(function () use ($validated, $request) {
            $entry = JournalEntry::create([
                'journal_number' => $validated['journal_number'],
                'journal_date' => $validated['journal_date'],
                'description' => $validated['description'],
                'reference_type' => $validated['reference_type'] ?? null,
                'reference_id' => $validated['reference_id'] ?? null,
                'status' => 'Draft',
                'created_by' => $request->user()?->id,
            ]);

            $entry->lines()->createMany($validated['lines']);

            return $entry;
        });

        return response()->json(
            $entry->load('lines.account'),
            201
        );
    }

    public function post(
        JournalEntry $journalEntry,
        JournalEntryService $service
    ): JsonResponse {
        $service->post($journalEntry);

        return response()->json(
            $journalEntry->fresh()->load('lines.account', 'creator')
        );
    }
}