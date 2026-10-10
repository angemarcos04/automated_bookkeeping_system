<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\CashCheckVoucher;
use App\Models\Rfp;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\Rule;

class CashCheckVoucherController extends Controller
{
    private function relations(): array
    {
        return ['rfp'];
    }

    private function rules(?CashCheckVoucher $voucher = null): array
    {
        $numberRule = Rule::unique(
            'cash_check_vouchers',
            'voucher_number'
        );

        if ($voucher) {
            $numberRule->ignore($voucher->id);
        }

        return [
            'voucher_number' => [
                $voucher ? 'sometimes' : 'required',
                'string',
                'max:50',
                $numberRule,
            ],

            'rfp_id' => [
                $voucher ? 'sometimes' : 'required',
                'integer',
                'exists:rfps,id',
            ],

            'voucher_date' => [
                $voucher ? 'sometimes' : 'required',
                'date',
            ],

            'payment_method' => [
                $voucher ? 'sometimes' : 'required',
                Rule::in(['Cash', 'Check']),
            ],

            'check_number' => [
                'sometimes',
                'nullable',
                'string',
                'max:100',
            ],

            'check_date' => [
                'sometimes',
                'nullable',
                'date',
            ],

            'bank_name' => [
                'sometimes',
                'nullable',
                'string',
                'max:150',
            ],

            'payee' => [
                $voucher ? 'sometimes' : 'required',
                'string',
                'max:150',
            ],

            'amount' => [
                $voucher ? 'sometimes' : 'required',
                'numeric',
                'min:0.01',
            ],

            'currency' => [
                'sometimes',
                'required',
                'string',
                'max:10',
            ],

            'status' => [
                'sometimes',
                Rule::in([
                    'Draft',
                    'Approved',
                    'Released',
                    'Paid',
                    'Cancelled',
                ]),
            ],

            'remarks' => [
                'sometimes',
                'nullable',
                'string',
            ],

            'prepared_by' => [
                'sometimes',
                'nullable',
                'string',
                'max:150',
            ],

            'prepared_date' => [
                'sometimes',
                'nullable',
                'date',
            ],

            'approved_by' => [
                'sometimes',
                'nullable',
                'string',
                'max:150',
            ],

            'approved_date' => [
                'sometimes',
                'nullable',
                'date',
            ],

            'released_by' => [
                'sometimes',
                'nullable',
                'string',
                'max:150',
            ],

            'released_date' => [
                'sometimes',
                'nullable',
                'date',
            ],

            'received_by' => [
                'sometimes',
                'nullable',
                'string',
                'max:150',
            ],

            'received_date' => [
                'sometimes',
                'nullable',
                'date',
            ],
        ];
    }

    private function validatePaymentMethod(array $data): void
    {
        if (($data['payment_method'] ?? null) === 'Cash') {
            if (!empty($data['check_number']) ||
                !empty($data['check_date']) ||
                !empty($data['bank_name'])) {
                abort(
                    response()->json([
                        'success' => false,
                        'message' => 'Cash vouchers must not contain check details.',
                    ], 422)
                );
            }
        }

        if (($data['payment_method'] ?? null) === 'Check') {
            if (empty($data['check_number']) ||
                empty($data['check_date']) ||
                empty($data['bank_name'])) {
                abort(
                    response()->json([
                        'success' => false,
                        'message' => 'Check vouchers require a check number, check date, and bank name.',
                    ], 422)
                );
            }
        }
    }

    public function index()
    {
        return response()->json([
            'success' => true,
            'data' => CashCheckVoucher::with($this->relations())
                ->latest()
                ->get(),
        ]);
    }

    public function store(Request $request)
    {
        $validated = $request->validate($this->rules());

        $this->validatePaymentMethod($validated);

        $rfp = Rfp::findOrFail($validated['rfp_id']);

        if ($rfp->status !== 'Approved') {
            return response()->json([
                'success' => false,
                'message' => 'A voucher can only be created from an approved RFP.',
            ], 422);
        }

        if (bccomp(
            (string) $validated['amount'],
            (string) $rfp->amount,
            2
        ) !== 0) {
            return response()->json([
                'success' => false,
                'message' => 'Voucher amount must match the approved RFP amount.',
            ], 422);
        }

        $voucher = DB::transaction(function () use ($validated) {
            return CashCheckVoucher::create($validated);
        });

        return response()->json([
            'success' => true,
            'message' => 'Cash/Check Voucher created successfully.',
            'data' => $voucher->load($this->relations()),
        ], 201);
    }

    public function show(CashCheckVoucher $cashCheckVoucher)
    {
        return response()->json([
            'success' => true,
            'data' => $cashCheckVoucher->load($this->relations()),
        ]);
    }

    public function update(
        Request $request,
        CashCheckVoucher $cashCheckVoucher
    ) {
        if (in_array($cashCheckVoucher->status, ['Released', 'Paid', 'Cancelled'], true)) {
            return response()->json([
                'success' => false,
                'message' => 'Released, paid, or cancelled vouchers cannot be edited.',
            ], 422);
        }

        $validated = $request->validate(
            $this->rules($cashCheckVoucher)
        );

        $this->validatePaymentMethod(array_merge(
            $cashCheckVoucher->toArray(),
            $validated
        ));

        $rfpId = $validated['rfp_id'] ?? $cashCheckVoucher->rfp_id;
        $amount = $validated['amount'] ?? $cashCheckVoucher->amount;

        $rfp = Rfp::findOrFail($rfpId);

        if ($rfp->status !== 'Approved') {
            return response()->json([
                'success' => false,
                'message' => 'The linked RFP must remain approved.',
            ], 422);
        }

        if (bccomp((string) $amount, (string) $rfp->amount, 2) !== 0) {
            return response()->json([
                'success' => false,
                'message' => 'Voucher amount must match the approved RFP amount.',
            ], 422);
        }

        $cashCheckVoucher->update($validated);

        return response()->json([
            'success' => true,
            'message' => 'Cash/Check Voucher updated successfully.',
            'data' => $cashCheckVoucher->fresh()->load($this->relations()),
        ]);
    }

    public function destroy(CashCheckVoucher $cashCheckVoucher)
    {
        if (in_array($cashCheckVoucher->status, ['Released', 'Paid'], true)) {
            return response()->json([
                'success' => false,
                'message' => 'Released or paid vouchers cannot be deleted.',
            ], 422);
        }

        $cashCheckVoucher->delete();

        return response()->json([
            'success' => true,
            'message' => 'Cash/Check Voucher deleted successfully.',
        ]);
    }
}