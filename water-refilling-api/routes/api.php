<?php

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Facades\Route;
use App\Models\User;

use App\Http\Controllers\Api\DashboardController;
use App\Http\Controllers\Api\CustomerController;
use App\Http\Controllers\Api\VendorController;
use App\Http\Controllers\Api\EmployeeController;
use App\Http\Controllers\Api\DepartmentController;
use App\Http\Controllers\Api\FundController;
use App\Http\Controllers\Api\FixedAssetController;
use App\Http\Controllers\Api\CashReceiptController;
use App\Http\Controllers\Api\CashDisbursementController;
use App\Http\Controllers\Api\RfpController;
use App\Http\Controllers\Api\InvoiceController;
use App\Http\Controllers\Api\CashCheckVoucherController;
use App\Http\Controllers\Api\JournalEntryController;
use App\Http\Controllers\Api\GeneralLedgerController;
use App\Http\Controllers\Api\TrialBalanceController;
use App\Http\Controllers\Api\AccountController;
use App\Http\Controllers\Api\LapsingScheduleController;

/*
|--------------------------------------------------------------------------
| Authentication
|--------------------------------------------------------------------------
*/



Route::post('/login', function (Request $request) {
    $request->validate([
        'email' => ['required', 'email'],
        'password' => ['required'],
    ]);

    $user = User::where('email', $request->email)->first();

    if (!$user || !Hash::check($request->password, $user->password)) {
        return response()->json([
            'success' => false,
            'message' => 'Invalid email or password.',
        ], 401);
    }

    $token = $user->createToken('vbnet-desktop')->plainTextToken;

    return response()->json([
        'success' => true,
        'message' => 'Login successful.',
        'token' => $token,
        'token_type' => 'Bearer',
        'user' => [
            'id' => $user->id,
            'name' => $user->name,
            'email' => $user->email,
            'roles' => $user->getRoleNames(),
        ],
    ]);
});

Route::middleware('auth:sanctum')->group(function () {

/*
|--------------------------------------------------------------------------
| Dashboard
|--------------------------------------------------------------------------
*/

Route::get('/dashboard', [DashboardController::class, 'index']);

/*
|--------------------------------------------------------------------------
| Customers
|--------------------------------------------------------------------------
*/

Route::get('/customers', [CustomerController::class, 'index']);
Route::post('/customers', [CustomerController::class, 'store']);
Route::get('/customers/{customer}', [CustomerController::class, 'show']);
Route::put('/customers/{customer}', [CustomerController::class, 'update']);
Route::delete('/customers/{customer}', [CustomerController::class, 'destroy']);

/*
|--------------------------------------------------------------------------
| Vendors / Payees
|--------------------------------------------------------------------------
*/

Route::get('/vendors', [VendorController::class, 'index']);
Route::post('/vendors', [VendorController::class, 'store']);
Route::get('/vendors/{vendor}', [VendorController::class, 'show']);
Route::put('/vendors/{vendor}', [VendorController::class, 'update']);
Route::delete('/vendors/{vendor}', [VendorController::class, 'destroy']);

/*
|--------------------------------------------------------------------------
| Employees
|--------------------------------------------------------------------------
*/

Route::get('/employees', [EmployeeController::class, 'index']);
Route::post('/employees', [EmployeeController::class, 'store']);
Route::get('/employees/{employee}', [EmployeeController::class, 'show']);
Route::put('/employees/{employee}', [EmployeeController::class, 'update']);
Route::delete('/employees/{employee}', [EmployeeController::class, 'destroy']);

/*
|--------------------------------------------------------------------------
| Departments / Programs
|--------------------------------------------------------------------------
*/

Route::get('/departments', [DepartmentController::class, 'index']);
Route::post('/departments', [DepartmentController::class, 'store']);
Route::get('/departments/{department}', [DepartmentController::class, 'show']);
Route::put('/departments/{department}', [DepartmentController::class, 'update']);
Route::delete('/departments/{department}', [DepartmentController::class, 'destroy']);

/*
|--------------------------------------------------------------------------
| Funds
|--------------------------------------------------------------------------
*/

Route::get('/funds', [FundController::class, 'index']);
Route::post('/funds', [FundController::class, 'store']);
Route::get('/funds/{fund}', [FundController::class, 'show']);
Route::put('/funds/{fund}', [FundController::class, 'update']);
Route::delete('/funds/{fund}', [FundController::class, 'destroy']);

/*
|--------------------------------------------------------------------------
| Fixed Assets
|--------------------------------------------------------------------------
*/

Route::get('/fixed-assets', [FixedAssetController::class, 'index']);
Route::post('/fixed-assets', [FixedAssetController::class, 'store']);
Route::get('/fixed-assets/{fixedAsset}', [FixedAssetController::class, 'show']);
Route::put('/fixed-assets/{fixedAsset}', [FixedAssetController::class, 'update']);
Route::delete('/fixed-assets/{fixedAsset}', [FixedAssetController::class, 'destroy']);

/*
|--------------------------------------------------------------------------
| Cash Receipts Book
|--------------------------------------------------------------------------
*/

Route::get('/cash-receipts', [CashReceiptController::class, 'index']);
Route::post('/cash-receipts', [CashReceiptController::class, 'store']);
Route::get('/cash-receipts/{cashReceipt}', [CashReceiptController::class, 'show']);
Route::put('/cash-receipts/{cashReceipt}', [CashReceiptController::class, 'update']);
Route::delete('/cash-receipts/{cashReceipt}', [CashReceiptController::class, 'destroy']);

/*
|--------------------------------------------------------------------------
| Cash Disbursements Book
|--------------------------------------------------------------------------
*/

Route::get('/cash-disbursements', [CashDisbursementController::class, 'index']);
Route::post('/cash-disbursements', [CashDisbursementController::class, 'store']);
Route::get('/cash-disbursements/{cashDisbursement}', [CashDisbursementController::class, 'show']);
Route::put('/cash-disbursements/{cashDisbursement}', [CashDisbursementController::class, 'update']);
Route::delete('/cash-disbursements/{cashDisbursement}', [CashDisbursementController::class, 'destroy']);

/*
|--------------------------------------------------------------------------
| Request for Payment (RFP)
|--------------------------------------------------------------------------
*/

Route::get('/rfps', [RfpController::class, 'index']);
Route::post('/rfps', [RfpController::class, 'store']);
Route::get('/rfps/{rfp}', [RfpController::class, 'show']);
Route::put('/rfps/{rfp}', [RfpController::class, 'update']);
Route::delete('/rfps/{rfp}', [RfpController::class, 'destroy']);

/*
|--------------------------------------------------------------------------
| Invoices
|--------------------------------------------------------------------------
*/

Route::get('/invoices', [InvoiceController::class, 'index']);
Route::post('/invoices', [InvoiceController::class, 'store']);
Route::get('/invoices/{invoice}', [InvoiceController::class, 'show']);
Route::put('/invoices/{invoice}', [InvoiceController::class, 'update']);
Route::delete('/invoices/{invoice}', [InvoiceController::class, 'destroy']);

/*
|--------------------------------------------------------------------------
| Cash/Check Vouchers
|--------------------------------------------------------------------------
*/

Route::get('/cash-check-vouchers', [CashCheckVoucherController::class, 'index']);
Route::post('/cash-check-vouchers', [CashCheckVoucherController::class, 'store']);
Route::get('/cash-check-vouchers/{cashCheckVoucher}', [CashCheckVoucherController::class, 'show']);
Route::put('/cash-check-vouchers/{cashCheckVoucher}', [CashCheckVoucherController::class, 'update']);
Route::delete('/cash-check-vouchers/{cashCheckVoucher}', [CashCheckVoucherController::class, 'destroy']);

/*
|--------------------------------------------------------------------------
| Journal Entries
|--------------------------------------------------------------------------
*/

Route::get('/journal-entries', [JournalEntryController::class, 'index']);
Route::get('/journal-entries/{journalEntry}', [JournalEntryController::class, 'show']);
Route::post('/journal-entries', [JournalEntryController::class, 'store']);
Route::post('/journal-entries/{journalEntry}/post', [JournalEntryController::class, 'post']);

/*
|--------------------------------------------------------------------------
| Accounting Reports
|--------------------------------------------------------------------------
*/

Route::get('/general-ledger', [GeneralLedgerController::class, 'index']);
Route::get('/trial-balance', [TrialBalanceController::class, 'index']);

/*
|--------------------------------------------------------------------------
| Chart of Accounts
|--------------------------------------------------------------------------
*/

Route::get('/accounts', [AccountController::class, 'index']);
Route::post('/accounts', [AccountController::class, 'store']);
Route::get('/accounts/{account}', [AccountController::class, 'show']);
Route::put('/accounts/{account}', [AccountController::class, 'update']);
Route::patch('/accounts/{account}', [AccountController::class, 'update']);
Route::delete('/accounts/{account}', [AccountController::class, 'destroy']);

/*
|--------------------------------------------------------------------------
| Lasping Schedule Report
|--------------------------------------------------------------------------
*/

Route::get('/reports/lapsing-schedule', [LapsingScheduleController::class, 'index']);
});