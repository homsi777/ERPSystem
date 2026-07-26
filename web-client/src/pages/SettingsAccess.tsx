import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  createIdentityRole,
  createIdentityUser,
  getIdentityRoles,
  getIdentityUsers,
  getPermissionTree,
  getRolePermissions,
  updateIdentityUserRoles,
  updateRolePermissions
} from '../api/settings.ts';
import type {
  IdentityRoleListDto,
  IdentityUserListDto,
  PermissionModuleGroupDto
} from '../api/types.ts';
import { getApiErrorMessage } from '../lib/apiError.ts';
import { AppShell } from '../components/AppShell.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { Modal } from '../components/Modal.tsx';

type SettingsAccessTab = 'users' | 'roles';

export function SettingsAccessPage() {
  const [tab, setTab] = useState<SettingsAccessTab>('users');
  const [notice, setNotice] = useState('');

  return (
    <AppShell title="الإعدادات">
      <div className="settings-access__heading">
        <div>
          <h2>المستخدمون والصلاحيات</h2>
          <p className="muted-line">إنشاء حسابات الموظفين وتحديد أدوارهم وصلاحيات كل دور.</p>
        </div>
        <Link className="ghost-button settings-access__sessions-link" to="/settings/user-sessions">
          حالة المستخدمين
        </Link>
      </div>

      <div className="tab-strip settings-access__tabs" role="tablist" aria-label="إدارة الوصول">
        <button
          type="button"
          className={`filter-chip ${tab === 'users' ? 'filter-chip--active' : ''}`}
          onClick={() => setTab('users')}
          role="tab"
          aria-selected={tab === 'users'}
        >
          المستخدمون
        </button>
        <button
          type="button"
          className={`filter-chip ${tab === 'roles' ? 'filter-chip--active' : ''}`}
          onClick={() => setTab('roles')}
          role="tab"
          aria-selected={tab === 'roles'}
        >
          الأدوار والصلاحيات
        </button>
      </div>

      {notice ? <div className="toast toast--success" role="status">{notice}</div> : null}

      {tab === 'users' ? <UsersWorkspace onNotice={setNotice} /> : null}
      {tab === 'roles' ? <RolesWorkspace onNotice={setNotice} /> : null}
    </AppShell>
  );
}

function UsersWorkspace({ onNotice }: { onNotice: (message: string) => void }) {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [selectedUser, setSelectedUser] = useState<IdentityUserListDto | null>(null);
  const usersQuery = useQuery({
    queryKey: ['settings', 'identity-users'],
    queryFn: getIdentityUsers
  });
  const rolesQuery = useQuery({
    queryKey: ['settings', 'identity-roles'],
    queryFn: getIdentityRoles
  });

  async function refreshUsers() {
    await queryClient.invalidateQueries({ queryKey: ['settings', 'identity-users'] });
  }

  return (
    <section className="settings-access__workspace" aria-label="المستخدمون">
      <div className="toolbar-row toolbar-row--start">
        <div>
          <strong>حسابات المستخدمين</strong>
          <p className="muted-line">اضغط على مستخدم لتعديل الأدوار المسندة إليه.</p>
        </div>
        <button className="primary-button" type="button" onClick={() => setShowCreate((value) => !value)}>
          {showCreate ? 'إغلاق النموذج' : 'إضافة مستخدم'}
        </button>
      </div>

      {showCreate ? (
        <CreateUserForm
          roles={rolesQuery.data ?? []}
          onCreated={async () => {
            await refreshUsers();
            setShowCreate(false);
            onNotice('تم إنشاء المستخدم وربط الأدوار المحددة بنجاح.');
          }}
        />
      ) : null}

      {usersQuery.isLoading || rolesQuery.isLoading ? <LoadingState /> : null}
      {usersQuery.isError ? (
        <ErrorState message={getApiErrorMessage(usersQuery.error)} onRetry={() => void usersQuery.refetch()} />
      ) : null}
      {rolesQuery.isError ? (
        <ErrorState message={getApiErrorMessage(rolesQuery.error)} onRetry={() => void rolesQuery.refetch()} />
      ) : null}

      {usersQuery.data?.length ? (
        <>
          <div className="table-scroll desktop-only">
            <table className="data-table">
              <thead>
                <tr>
                  <th>المستخدم</th>
                  <th>الاسم</th>
                  <th>الأدوار</th>
                  <th>الحالة</th>
                </tr>
              </thead>
              <tbody>
                {usersQuery.data.map((user) => (
                  <tr className="clickable-row" key={user.id} onClick={() => setSelectedUser(user)}>
                    <td>{user.username}</td>
                    <td>{user.fullNameAr}</td>
                    <td>{user.roleNames.length ? user.roleNames.join('، ') : 'دون دور'}</td>
                    <td>
                      <span className={`status-pill ${user.isActive ? 'status-pill--green' : 'status-pill--amber'}`}>
                        {user.isActive ? 'نشط' : 'متوقف'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="card-list mobile-only">
            {usersQuery.data.map((user) => (
              <button
                type="button"
                className="line-card settings-user-card"
                key={user.id}
                onClick={() => setSelectedUser(user)}
              >
                <span className="line-card__head">
                  <strong>{user.fullNameAr}</strong>
                  <span className={`status-pill ${user.isActive ? 'status-pill--green' : 'status-pill--amber'}`}>
                    {user.isActive ? 'نشط' : 'متوقف'}
                  </span>
                </span>
                <span className="muted-line">{user.username}</span>
                <span>{user.roleNames.length ? user.roleNames.join('، ') : 'دون دور'}</span>
              </button>
            ))}
          </div>
        </>
      ) : null}

      {usersQuery.isSuccess && usersQuery.data.length === 0 ? (
        <EmptyState title="لا يوجد مستخدمون" description="أنشئ أول حساب موظف من الزر أعلاه." />
      ) : null}

      {selectedUser ? (
        <UserRolesModal
          user={selectedUser}
          roles={rolesQuery.data ?? []}
          onClose={() => setSelectedUser(null)}
          onSaved={async () => {
            await refreshUsers();
            setSelectedUser(null);
            onNotice('تم تحديث أدوار المستخدم بنجاح.');
          }}
        />
      ) : null}
    </section>
  );
}

function CreateUserForm({
  roles,
  onCreated
}: {
  roles: IdentityRoleListDto[];
  onCreated: () => Promise<void>;
}) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [fullNameAr, setFullNameAr] = useState('');
  const [fullNameEn, setFullNameEn] = useState('');
  const [roleIds, setRoleIds] = useState<string[]>([]);
  const [error, setError] = useState('');
  const createMutation = useMutation({
    mutationFn: createIdentityUser,
    onSuccess: onCreated,
    onError: (mutationError) => setError(getApiErrorMessage(mutationError))
  });

  function toggleRole(roleId: string) {
    setRoleIds((current) =>
      current.includes(roleId) ? current.filter((id) => id !== roleId) : [...current, roleId]);
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    if (!username.trim() || !password || !fullNameAr.trim()) {
      setError('اسم المستخدم وكلمة المرور والاسم بالعربي مطلوبة.');
      return;
    }
    createMutation.mutate({
      username: username.trim(),
      password,
      fullNameAr: fullNameAr.trim(),
      fullNameEn: fullNameEn.trim(),
      roleIds
    });
  }

  return (
    <form className="form-panel form-compact settings-access__create-form" onSubmit={handleSubmit}>
      <h2>بيانات المستخدم الجديد</h2>
      <div className="form-grid">
        <label>
          اسم المستخدم
          <input value={username} onChange={(event) => setUsername(event.target.value)} autoComplete="off" />
        </label>
        <label>
          كلمة المرور
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete="new-password"
          />
        </label>
        <label>
          الاسم بالعربي
          <input value={fullNameAr} onChange={(event) => setFullNameAr(event.target.value)} />
        </label>
        <label>
          الاسم بالإنجليزي (اختياري)
          <input value={fullNameEn} onChange={(event) => setFullNameEn(event.target.value)} />
        </label>
      </div>

      <fieldset className="settings-role-picker">
        <legend>الأدوار</legend>
        <div className="settings-role-picker__options">
          {roles.map((role) => (
            <label key={role.id}>
              <input
                type="checkbox"
                checked={roleIds.includes(role.id)}
                onChange={() => toggleRole(role.id)}
              />
              <span>{role.name}</span>
            </label>
          ))}
        </div>
      </fieldset>

      {error ? <div className="toast toast--error" role="alert">{error}</div> : null}
      <div className="compact-action-row">
        <button className="primary-button" type="submit" disabled={createMutation.isPending}>
          {createMutation.isPending ? 'جارٍ الإنشاء...' : 'إنشاء المستخدم'}
        </button>
      </div>
    </form>
  );
}

function UserRolesModal({
  user,
  roles,
  onClose,
  onSaved
}: {
  user: IdentityUserListDto;
  roles: IdentityRoleListDto[];
  onClose: () => void;
  onSaved: () => Promise<void>;
}) {
  const [roleIds, setRoleIds] = useState<string[]>(user.roleIds);
  const [error, setError] = useState('');
  const updateMutation = useMutation({
    mutationFn: () => updateIdentityUserRoles(user.id, roleIds),
    onSuccess: onSaved,
    onError: (mutationError) => setError(getApiErrorMessage(mutationError))
  });

  function toggleRole(roleId: string) {
    setRoleIds((current) =>
      current.includes(roleId) ? current.filter((id) => id !== roleId) : [...current, roleId]);
  }

  return (
    <Modal
      title={`أدوار ${user.fullNameAr}`}
      subtitle={`اسم المستخدم: ${user.username}`}
      onClose={updateMutation.isPending ? () => undefined : onClose}
    >
      <div className="settings-role-picker__options settings-role-picker__options--modal">
        {roles.map((role) => (
          <label key={role.id}>
            <input
              type="checkbox"
              checked={roleIds.includes(role.id)}
              onChange={() => toggleRole(role.id)}
              disabled={updateMutation.isPending}
            />
            <span>
              <strong>{role.name}</strong>
              {role.description ? <small>{role.description}</small> : null}
            </span>
          </label>
        ))}
      </div>
      {error ? <div className="toast toast--error" role="alert">{error}</div> : null}
      <div className="compact-action-row settings-access__modal-actions">
        <button
          className="primary-button"
          type="button"
          onClick={() => updateMutation.mutate()}
          disabled={updateMutation.isPending}
        >
          {updateMutation.isPending ? 'جارٍ الحفظ...' : 'حفظ الأدوار'}
        </button>
        <button className="ghost-button" type="button" onClick={onClose} disabled={updateMutation.isPending}>
          إلغاء
        </button>
      </div>
    </Modal>
  );
}

function RolesWorkspace({ onNotice }: { onNotice: (message: string) => void }) {
  const queryClient = useQueryClient();
  const [selectedRoleId, setSelectedRoleId] = useState('');
  const [selectedCodes, setSelectedCodes] = useState<string[]>([]);
  const [roleName, setRoleName] = useState('');
  const [roleDescription, setRoleDescription] = useState('');
  const [error, setError] = useState('');
  const rolesQuery = useQuery({
    queryKey: ['settings', 'identity-roles'],
    queryFn: getIdentityRoles
  });
  const permissionTreeQuery = useQuery({
    queryKey: ['settings', 'permission-tree'],
    queryFn: getPermissionTree
  });
  const rolePermissionsQuery = useQuery({
    queryKey: ['settings', 'role-permissions', selectedRoleId],
    queryFn: () => getRolePermissions(selectedRoleId),
    enabled: Boolean(selectedRoleId)
  });

  useEffect(() => {
    if (!selectedRoleId && rolesQuery.data?.length) {
      setSelectedRoleId(rolesQuery.data[0]!.id);
    }
  }, [rolesQuery.data, selectedRoleId]);

  useEffect(() => {
    if (rolePermissionsQuery.data) {
      setSelectedCodes(rolePermissionsQuery.data.permissionCodes);
      setError('');
    }
  }, [rolePermissionsQuery.data]);

  const selectedRole = rolesQuery.data?.find((role) => role.id === selectedRoleId);
  const allPermissionCodes = useMemo(
    () => (permissionTreeQuery.data ?? []).flatMap((module) => module.permissions.map((item) => item.code)),
    [permissionTreeQuery.data]
  );

  const createRoleMutation = useMutation({
    mutationFn: createIdentityRole,
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: ['settings', 'identity-roles'] });
      setRoleName('');
      setRoleDescription('');
      setSelectedRoleId(result.id);
      onNotice('تم إنشاء الدور. يمكنك الآن تحديد صلاحياته.');
    },
    onError: (mutationError) => setError(getApiErrorMessage(mutationError))
  });
  const savePermissionsMutation = useMutation({
    mutationFn: () => updateRolePermissions(selectedRoleId, selectedCodes),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['settings', 'identity-roles'] }),
        queryClient.invalidateQueries({ queryKey: ['settings', 'role-permissions', selectedRoleId] })
      ]);
      onNotice('تم حفظ صلاحيات الدور بنجاح.');
    },
    onError: (mutationError) => setError(getApiErrorMessage(mutationError))
  });

  function handleCreateRole(event: FormEvent) {
    event.preventDefault();
    setError('');
    if (!roleName.trim()) {
      setError('اسم الدور مطلوب.');
      return;
    }
    createRoleMutation.mutate({ name: roleName.trim(), description: roleDescription.trim() });
  }

  function togglePermission(code: string) {
    if (selectedRole?.isSystem) return;
    setSelectedCodes((current) =>
      current.includes(code) ? current.filter((item) => item !== code) : [...current, code]);
  }

  function toggleModule(module: PermissionModuleGroupDto) {
    if (selectedRole?.isSystem) return;
    const moduleCodes = module.permissions.map((permission) => permission.code);
    const everySelected = moduleCodes.every((code) => selectedCodes.includes(code));
    setSelectedCodes((current) => everySelected
      ? current.filter((code) => !moduleCodes.includes(code))
      : Array.from(new Set([...current, ...moduleCodes])));
  }

  return (
    <section className="settings-access__roles-layout" aria-label="الأدوار والصلاحيات">
      <aside className="settings-access__roles-sidebar">
        <div className="settings-access__section-title">
          <strong>الأدوار</strong>
          <span>{rolesQuery.data?.length ?? 0}</span>
        </div>

        {rolesQuery.isLoading ? <LoadingState /> : null}
        {rolesQuery.isError ? (
          <ErrorState message={getApiErrorMessage(rolesQuery.error)} onRetry={() => void rolesQuery.refetch()} />
        ) : null}

        <div className="settings-role-list">
          {(rolesQuery.data ?? []).map((role) => (
            <button
              type="button"
              className={role.id === selectedRoleId ? 'is-active' : ''}
              key={role.id}
              onClick={() => setSelectedRoleId(role.id)}
            >
              <span>
                <strong>{role.name}</strong>
                <small>{role.isSystem ? 'دور نظام' : `${role.permissionCount} صلاحية`}</small>
              </span>
            </button>
          ))}
        </div>

        <form className="settings-new-role" onSubmit={handleCreateRole}>
          <strong>دور جديد</strong>
          <input
            value={roleName}
            onChange={(event) => setRoleName(event.target.value)}
            placeholder="اسم الدور"
          />
          <input
            value={roleDescription}
            onChange={(event) => setRoleDescription(event.target.value)}
            placeholder="وصف مختصر (اختياري)"
          />
          <button className="ghost-button" type="submit" disabled={createRoleMutation.isPending}>
            {createRoleMutation.isPending ? 'جارٍ الإنشاء...' : 'إنشاء الدور'}
          </button>
        </form>
      </aside>

      <div className="settings-access__permissions">
        <div className="settings-access__permissions-head">
          <div>
            <h2>{selectedRole ? `صلاحيات ${selectedRole.name}` : 'اختر دوراً'}</h2>
            <p className="muted-line">حدد المهام التي يستطيع أصحاب هذا الدور تنفيذها.</p>
          </div>
          {!selectedRole?.isSystem ? (
            <div className="compact-action-row">
              <button
                className="ghost-button"
                type="button"
                onClick={() => setSelectedCodes(allPermissionCodes)}
                disabled={!selectedRoleId}
              >
                تحديد الكل
              </button>
              <button
                className="ghost-button"
                type="button"
                onClick={() => setSelectedCodes([])}
                disabled={!selectedRoleId}
              >
                إلغاء الكل
              </button>
            </div>
          ) : null}
        </div>

        {selectedRole?.isSystem ? (
          <div className="banner banner--warn">دور النظام يملك جميع الصلاحيات تلقائياً ولا يمكن تعديله.</div>
        ) : null}
        {permissionTreeQuery.isLoading || rolePermissionsQuery.isLoading ? <LoadingState /> : null}
        {permissionTreeQuery.isError ? (
          <ErrorState
            message={getApiErrorMessage(permissionTreeQuery.error)}
            onRetry={() => void permissionTreeQuery.refetch()}
          />
        ) : null}
        {rolePermissionsQuery.isError ? (
          <ErrorState
            message={getApiErrorMessage(rolePermissionsQuery.error)}
            onRetry={() => void rolePermissionsQuery.refetch()}
          />
        ) : null}

        <div className="settings-permission-tree">
          {(permissionTreeQuery.data ?? []).map((module) => {
            const moduleCodes = module.permissions.map((permission) => permission.code);
            const selectedCount = moduleCodes.filter((code) => selectedCodes.includes(code)).length;
            const moduleChecked = selectedCount === moduleCodes.length && moduleCodes.length > 0;
            return (
              <details key={module.moduleKey} open>
                <summary>
                  <label onClick={(event) => event.stopPropagation()}>
                    <input
                      type="checkbox"
                      checked={moduleChecked}
                      ref={(input) => {
                        if (input) input.indeterminate = selectedCount > 0 && !moduleChecked;
                      }}
                      onChange={() => toggleModule(module)}
                      disabled={!selectedRoleId || selectedRole?.isSystem}
                    />
                    <span>{module.moduleLabelAr}</span>
                  </label>
                  <small>{selectedCount}/{moduleCodes.length}</small>
                </summary>
                <div className="settings-permission-tree__items">
                  {module.permissions.map((permission) => (
                    <label key={permission.code}>
                      <input
                        type="checkbox"
                        checked={selectedCodes.includes(permission.code)}
                        onChange={() => togglePermission(permission.code)}
                        disabled={!selectedRoleId || selectedRole?.isSystem}
                      />
                      <span>{permission.labelAr}</span>
                    </label>
                  ))}
                </div>
              </details>
            );
          })}
        </div>

        {error ? <div className="toast toast--error" role="alert">{error}</div> : null}
        {!selectedRole?.isSystem ? (
          <div className="settings-access__save-bar">
            <span>{selectedCodes.length} صلاحية محددة</span>
            <button
              className="primary-button"
              type="button"
              onClick={() => savePermissionsMutation.mutate()}
              disabled={!selectedRoleId || savePermissionsMutation.isPending}
            >
              {savePermissionsMutation.isPending ? 'جارٍ الحفظ...' : 'حفظ الصلاحيات'}
            </button>
          </div>
        ) : null}
      </div>
    </section>
  );
}
