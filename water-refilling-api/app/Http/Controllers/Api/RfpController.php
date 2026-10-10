<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Rfp;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\Rule;

class RfpController extends Controller
{
    private function relations(): array
    {
        return ['account', 'user', 'details.account'];
    }

    private function rules(?Rfp $rfp = null): array
    {
        $numberRule = Rule::unique('rfps', 'rfp_number');

        if ($rfp) {
            $numberRule->ignore($rfp->id);
        }

        return [
            'rfp_number' => [$rfp ? 'sometimes' : 'required', 'string', 'max:30', $numberRule],
            'rfp_date' => [$rfp ? 'sometimes' : 'required', 'date'],
            'requested_by' => [$rfp ? 'sometimes' : 'required', 'string', 'max:150'],
            'payee' => [$rfp ? 'sometimes' : 'required', 'string', 'max:150'],
            'purpose' => [$rfp ? 'sometimes' : 'required', 'string'],
            'amount' => [$rfp ? 'sometimes' : 'required', 'numeric', 'min:0.01'],
            'status' => ['sometimes', Rule::in([
                'Draft', 'Submitted', 'Under Review', 'Approved',
                'Rejected', 'Paid', 'Posted',
            ])],
            'remarks' => ['sometimes', 'nullable', 'string'],
            'account_id' => ['sometimes', 'nullable', 'integer', 'exists:accounts,id'],
            'user_id' => ['sometimes', 'nullable', 'integer', 'exists:users,id'],

            'reference_invoice_no' => ['sometimes', 'nullable', 'string', 'max:100'],
            'tax_identification_number' => ['sometimes', 'nullable', 'string', 'max:30'],
            'currency' => ['sometimes', 'required', 'string', 'max:10'],

            'prepared_by' => ['sometimes', 'nullable', 'string', 'max:150'],
            'prepared_by_date' => ['sometimes', 'nullable', 'date'],
            'reviewed_by' => ['sometimes', 'nullable', 'string', 'max:150'],
            'reviewed_by_date' => ['sometimes', 'nullable', 'date'],
            'approved_by' => ['sometimes', 'nullable', 'string', 'max:150'],
            'approved_by_date' => ['sometimes', 'nullable', 'date'],
            'requested_by_date' => ['sometimes', 'nullable', 'date'],
            'received_by' => ['sometimes', 'nullable', 'string', 'max:150'],
            'received_by_date' => ['sometimes', 'nullable', 'date'],

            'details' => ['sometimes', 'array'],
            'details.*.account_id' => ['nullable', 'integer', 'exists:accounts,id'],
            'details.*.account_code' => ['nullable', 'string', 'max:30'],
            'details.*.account_title' => ['nullable', 'string', 'max:150'],
            'details.*.location_project' => ['nullable', 'string', 'max:150'],
            'details.*.debit_amount' => ['required_with:details', 'numeric', 'min:0'],
            'details.*.credit_amount' => ['required_with:details', 'numeric', 'min:0'],
        ];
    }

    public function index()
    {
        return response()->json([
            'success' => true,
            'data' => Rfp::with($this->relations())->latest()->get(),
        ]);
    }

    public function store(Request $request)
    {
        $validated = $request->validate($this->rules());

        $details = $validated['details'] ?? [];
        unset($validated['details']);

        $rfp = DB::transaction(function () use ($validated, $details) {
            $rfp = Rfp::create($validated);

            if (!empty($details)) {
                $rfp->details()->createMany($details);
            }

            return $rfp;
        });

        return response()->json([
            'success' => true,
            'message' => 'RFP / voucher created successfully.',
            'data' => $rfp->load($this->relations()),
        ], 201);
    }

    public function show(Rfp $rfp)
    {
        return response()->json([
            'success' => true,
            'data' => $rfp->load($this->relations()),
        ]);
    }

    public function update(Request $request, Rfp $rfp)
    {
        $validated = $request->validate($this->rules($rfp));

        $hasDetails = array_key_exists('details', $validated);
        $details = $validated['details'] ?? [];
        unset($validated['details']);

        DB::transaction(function () use ($rfp, $validated, $hasDetails, $details) {
            $rfp->update($validated);

            if ($hasDetails) {
                $rfp->details()->delete();

                if (!empty($details)) {
                    $rfp->details()->createMany($details);
                }
            }
        });

        return response()->json([
            'success' => true,
            'message' => 'RFP / voucher updated successfully.',
            'data' => $rfp->fresh()->load($this->relations()),
        ]);
    }

    public function destroy(Rfp $rfp)
    {
        DB::transaction(function () use ($rfp) {
            $rfp->details()->delete();
            $rfp->delete();
        });

        return response()->json([
            'success' => true,
            'message' => 'RFP / voucher deleted successfully.',
        ]);
    }
}