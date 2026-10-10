<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Invoice;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\Rule;

class InvoiceController extends Controller
{
    public function index(): JsonResponse
    {
        $invoices = Invoice::with('items')
            ->latest()
            ->paginate(15);

        return response()->json($invoices);
    }

    public function store(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'invoice_number' => ['required', 'string', 'max:255', 'unique:invoices,invoice_number'],
            'invoice_date' => ['required', 'date'],
            'currency' => ['nullable', 'string', 'max:10'],
            'customer_name' => ['required', 'string', 'max:255'],
            'customer_address' => ['nullable', 'string'],
            'tax_identification_number' => ['nullable', 'string', 'max:255'],
            'prepared_by' => ['nullable', 'string', 'max:255'],
            'prepared_date' => ['nullable', 'date'],
            'reviewed_by' => ['nullable', 'string', 'max:255'],
            'reviewed_date' => ['nullable', 'date'],
            'approved_by' => ['nullable', 'string', 'max:255'],
            'approved_date' => ['nullable', 'date'],
            'items' => ['required', 'array', 'min:1'],
            'items.*.nature_of_service' => ['required', 'string'],
            'items.*.quantity' => ['required', 'numeric', 'gt:0'],
            'items.*.unit_cost_price' => ['required', 'numeric', 'min:0'],
            'items.*.amount' => ['required', 'numeric', 'min:0'],
        ]);

        $invoice = DB::transaction(function () use ($validated) {
            $invoice = Invoice::create([
                'invoice_number' => $validated['invoice_number'],
                'invoice_date' => $validated['invoice_date'],
                'currency' => $validated['currency'] ?? 'PHP',
                'customer_name' => $validated['customer_name'],
                'customer_address' => $validated['customer_address'] ?? null,
                'tax_identification_number' => $validated['tax_identification_number'] ?? null,
                'prepared_by' => $validated['prepared_by'] ?? null,
                'prepared_date' => $validated['prepared_date'] ?? null,
                'reviewed_by' => $validated['reviewed_by'] ?? null,
                'reviewed_date' => $validated['reviewed_date'] ?? null,
                'approved_by' => $validated['approved_by'] ?? null,
                'approved_date' => $validated['approved_date'] ?? null,
            ]);

            $invoice->items()->createMany($validated['items']);

            return $invoice->load('items');
        });

        return response()->json([
            'message' => 'Invoice created successfully.',
            'data' => $invoice,
        ], 201);
    }

    public function show(Invoice $invoice): JsonResponse
    {
        return response()->json($invoice->load('items'));
    }

    public function update(Request $request, Invoice $invoice): JsonResponse
    {
        $validated = $request->validate([
            'invoice_number' => [
                'sometimes', 'required', 'string', 'max:255',
                Rule::unique('invoices', 'invoice_number')->ignore($invoice->id),
            ],
            'invoice_date' => ['sometimes', 'required', 'date'],
            'currency' => ['sometimes', 'nullable', 'string', 'max:10'],
            'customer_name' => ['sometimes', 'required', 'string', 'max:255'],
            'customer_address' => ['sometimes', 'nullable', 'string'],
            'tax_identification_number' => ['sometimes', 'nullable', 'string', 'max:255'],
            'prepared_by' => ['sometimes', 'nullable', 'string', 'max:255'],
            'prepared_date' => ['sometimes', 'nullable', 'date'],
            'reviewed_by' => ['sometimes', 'nullable', 'string', 'max:255'],
            'reviewed_date' => ['sometimes', 'nullable', 'date'],
            'approved_by' => ['sometimes', 'nullable', 'string', 'max:255'],
            'approved_date' => ['sometimes', 'nullable', 'date'],
            'items' => ['sometimes', 'array', 'min:1'],
            'items.*.nature_of_service' => ['required_with:items', 'string'],
            'items.*.quantity' => ['required_with:items', 'numeric', 'gt:0'],
            'items.*.unit_cost_price' => ['required_with:items', 'numeric', 'min:0'],
            'items.*.amount' => ['required_with:items', 'numeric', 'min:0'],
        ]);

        $invoice = DB::transaction(function () use ($validated, $invoice) {
            $invoice->update(collect($validated)->except('items')->all());

            if (array_key_exists('items', $validated)) {
                $invoice->items()->delete();
                $invoice->items()->createMany($validated['items']);
            }

            return $invoice->load('items');
        });

        return response()->json([
            'message' => 'Invoice updated successfully.',
            'data' => $invoice,
        ]);
    }

    public function destroy(Invoice $invoice): JsonResponse
    {
        $invoice->delete();

        return response()->json([
            'message' => 'Invoice deleted successfully.',
        ]);
    }
}