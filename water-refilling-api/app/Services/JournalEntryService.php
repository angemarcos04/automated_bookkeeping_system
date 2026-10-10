<?php

namespace App\Services;

use App\Models\JournalEntry;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\ValidationException;

class JournalEntryService
{
    public function validateForPosting(JournalEntry $entry): void
    {
    if (! $entry->relationLoaded('lines')) {
    $entry->load('lines');
}

        if ($entry->lines->count() < 2) {
            throw ValidationException::withMessages([
                'lines' => 'A journal entry must have at least two lines.',
            ]);
        }

        foreach ($entry->lines as $line) {
            $debit = (float) $line->debit_amount;
            $credit = (float) $line->credit_amount;

            if ($debit < 0 || $credit < 0) {
                throw ValidationException::withMessages([
                    'lines' => 'Debit and credit amounts cannot be negative.',
                ]);
            }

            if (($debit > 0 && $credit > 0) ||
                ($debit == 0 && $credit == 0)) {
                throw ValidationException::withMessages([
                    'lines' => 'Each line must contain either a debit or a credit, not both or neither.',
                ]);
            }
        }

        $totalDebits = $entry->lines->sum(
            fn ($line) => (float) $line->debit_amount
        );

        $totalCredits = $entry->lines->sum(
            fn ($line) => (float) $line->credit_amount
        );

        if (round($totalDebits, 2) !== round($totalCredits, 2)) {
            throw ValidationException::withMessages([
                'lines' => 'Total debits must equal total credits before posting.',
            ]);
        }
    }

    public function post(JournalEntry $entry): void
    {
        DB::transaction(function () use ($entry) {
            $lockedEntry = JournalEntry::query()
                ->lockForUpdate()
                ->findOrFail($entry->id);

            if ($lockedEntry->status !== 'Draft') {
                throw ValidationException::withMessages([
                    'status' => 'Only draft journal entries can be posted.',
                ]);
            }

            $this->validateForPosting($lockedEntry);

            $lockedEntry->status = 'Posted';
            $lockedEntry->posted_at = now();
            $lockedEntry->save();
        });
    }
}