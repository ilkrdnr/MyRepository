create extension if not exists "pgcrypto";

create table if not exists public.user_permissions (
    user_id uuid primary key references auth.users(id) on delete cascade,
    email text not null,
    is_admin boolean not null default false,
    can_view_pool boolean not null default true,
    can_upload_pool boolean not null default true,
    can_edit_reports boolean not null default true,
    updated_at timestamptz not null default now(),
    updated_by uuid
);

create table if not exists public.reports (
    id uuid primary key default gen_random_uuid(),
    report_name text not null,
    report_code text,
    description text,
    sql_content text,
    pascal_content text,
    file_path text not null,
    file_url text not null,
    source_created_at timestamptz,
    source_modified_at timestamptz,
    owner_id uuid references auth.users(id),
    owner_email text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists idx_reports_created_at on public.reports (created_at desc);
create index if not exists idx_reports_owner_id on public.reports (owner_id);
create index if not exists idx_user_permissions_email on public.user_permissions (email);

create or replace function public.touch_updated_at()
returns trigger
language plpgsql
as $$
begin
    new.updated_at = now();
    return new;
end;
$$;

drop trigger if exists trg_reports_touch_updated_at on public.reports;
create trigger trg_reports_touch_updated_at
before update on public.reports
for each row
execute function public.touch_updated_at();

drop trigger if exists trg_permissions_touch_updated_at on public.user_permissions;
create trigger trg_permissions_touch_updated_at
before update on public.user_permissions
for each row
execute function public.touch_updated_at();

create or replace function public.is_admin_user(check_user uuid)
returns boolean
language sql
stable
security definer
set search_path = public
as $$
    select exists (
        select 1
        from public.user_permissions up
        where up.user_id = check_user
          and up.is_admin = true
    );
$$;

create or replace function public.user_can_view_pool(check_user uuid)
returns boolean
language sql
stable
security definer
set search_path = public
as $$
    select coalesce((
        select up.can_view_pool
        from public.user_permissions up
        where up.user_id = check_user
        limit 1
    ), true);
$$;

create or replace function public.user_can_upload_pool(check_user uuid)
returns boolean
language sql
stable
security definer
set search_path = public
as $$
    select coalesce((
        select up.can_upload_pool
        from public.user_permissions up
        where up.user_id = check_user
        limit 1
    ), true);
$$;

create or replace function public.user_can_edit_reports(check_user uuid)
returns boolean
language sql
stable
security definer
set search_path = public
as $$
    select coalesce((
        select up.can_edit_reports
        from public.user_permissions up
        where up.user_id = check_user
        limit 1
    ), true);
$$;

grant execute on function public.is_admin_user(uuid) to authenticated;
grant execute on function public.user_can_view_pool(uuid) to authenticated;
grant execute on function public.user_can_upload_pool(uuid) to authenticated;
grant execute on function public.user_can_edit_reports(uuid) to authenticated;

alter table public.user_permissions enable row level security;
alter table public.reports enable row level security;

drop policy if exists "permissions_self_or_admin_read" on public.user_permissions;
create policy "permissions_self_or_admin_read"
on public.user_permissions
for select
to authenticated
using (
    auth.uid() = user_id
    or public.is_admin_user(auth.uid())
);

drop policy if exists "permissions_self_insert_defaults" on public.user_permissions;
create policy "permissions_self_insert_defaults"
on public.user_permissions
for insert
to authenticated
with check (
    auth.uid() = user_id
    and is_admin = false
    and can_view_pool = true
    and can_upload_pool = true
    and can_edit_reports = true
);

drop policy if exists "permissions_admin_update" on public.user_permissions;
create policy "permissions_admin_update"
on public.user_permissions
for update
to authenticated
using (public.is_admin_user(auth.uid()))
with check (public.is_admin_user(auth.uid()));

drop policy if exists "permissions_admin_delete" on public.user_permissions;
create policy "permissions_admin_delete"
on public.user_permissions
for delete
to authenticated
using (public.is_admin_user(auth.uid()));

drop policy if exists "reports_permissioned_read" on public.reports;
create policy "reports_permissioned_read"
on public.reports
for select
to authenticated
using (
    owner_id = auth.uid()
    or public.is_admin_user(auth.uid())
    or public.user_can_view_pool(auth.uid())
);

drop policy if exists "reports_permissioned_insert" on public.reports;
create policy "reports_permissioned_insert"
on public.reports
for insert
to authenticated
with check (
    auth.uid() = owner_id
    and public.user_can_upload_pool(auth.uid())
);

drop policy if exists "reports_permissioned_update" on public.reports;
create policy "reports_permissioned_update"
on public.reports
for update
to authenticated
using (
    public.is_admin_user(auth.uid())
    or (
        auth.uid() = owner_id
        and public.user_can_edit_reports(auth.uid())
    )
)
with check (
    public.is_admin_user(auth.uid())
    or (
        auth.uid() = owner_id
        and public.user_can_edit_reports(auth.uid())
    )
);

drop policy if exists "reports_permissioned_delete" on public.reports;
create policy "reports_permissioned_delete"
on public.reports
for delete
to authenticated
using (
    public.is_admin_user(auth.uid())
    or (
        auth.uid() = owner_id
        and public.user_can_edit_reports(auth.uid())
    )
);

insert into storage.buckets (id, name, public)
values ('frp-files', 'frp-files', true)
on conflict (id) do nothing;

drop policy if exists "frp_files_permissioned_read" on storage.objects;
create policy "frp_files_permissioned_read"
on storage.objects
for select
to authenticated
using (
    bucket_id = 'frp-files'
    and public.user_can_view_pool(auth.uid())
);

drop policy if exists "frp_files_permissioned_insert" on storage.objects;
create policy "frp_files_permissioned_insert"
on storage.objects
for insert
to authenticated
with check (
    bucket_id = 'frp-files'
    and public.user_can_upload_pool(auth.uid())
);

drop policy if exists "frp_files_permissioned_update" on storage.objects;
create policy "frp_files_permissioned_update"
on storage.objects
for update
to authenticated
using (
    bucket_id = 'frp-files'
    and (
        (owner = auth.uid() and public.user_can_edit_reports(auth.uid()))
        or public.is_admin_user(auth.uid())
    )
)
with check (
    bucket_id = 'frp-files'
    and (
        (owner = auth.uid() and public.user_can_edit_reports(auth.uid()))
        or public.is_admin_user(auth.uid())
    )
);

drop policy if exists "frp_files_permissioned_delete" on storage.objects;
create policy "frp_files_permissioned_delete"
on storage.objects
for delete
to authenticated
using (
    bucket_id = 'frp-files'
    and (
        (owner = auth.uid() and public.user_can_edit_reports(auth.uid()))
        or public.is_admin_user(auth.uid())
    )
);