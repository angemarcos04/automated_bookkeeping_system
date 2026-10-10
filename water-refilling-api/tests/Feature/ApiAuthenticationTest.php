<?php

namespace Tests\Feature;

use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\Hash;
use Spatie\Permission\Models\Permission;
use Tests\TestCase;

class ApiAuthenticationTest extends TestCase
{
    use RefreshDatabase;

    public function test_private_api_rejects_requests_without_a_token(): void
    {
        $this->getJson('/api/accounts')->assertUnauthorized();
    }

    public function test_valid_login_issues_token_that_can_access_authorized_api(): void
    {
        $user = User::create([
            'name' => 'Test Accountant',
            'email' => 'accountant@example.test',
            'password' => Hash::make('correct-password'),
        ]);

        Permission::create([
            'name' => 'accounts.view',
            'guard_name' => 'web',
        ]);
        $user->givePermissionTo('accounts.view');

        $login = $this->postJson('/api/login', [
            'email' => $user->email,
            'password' => 'correct-password',
        ])->assertOk()
            ->assertJsonStructure(['token', 'user' => ['id', 'email', 'roles']]);

        $token = $login->json('token');
        $this->assertNotEmpty($token);

        $this->withToken($token)
            ->getJson('/api/accounts')
            ->assertOk()
            ->assertJsonStructure(['success', 'data']);
    }

    public function test_authenticated_user_without_permission_cannot_access_route(): void
    {
        $user = User::create([
            'name' => 'Restricted Accountant',
            'email' => 'restricted@example.test',
            'password' => Hash::make('correct-password'),
        ]);

        $token = $user->createToken('test')->plainTextToken;

        $this->withToken($token)
            ->getJson('/api/accounts')
            ->assertForbidden();
    }

    public function test_logout_revokes_the_current_token(): void
    {
        $user = User::create([
            'name' => 'Test Accountant',
            'email' => 'logout@example.test',
            'password' => Hash::make('correct-password'),
        ]);

        $token = $user->createToken('test')->plainTextToken;

        $this->withToken($token)
            ->postJson('/api/logout')
            ->assertOk();

        $this->assertCount(0, $user->tokens);
    }
}
