create extension if not exists "pgcrypto";

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
    created_at timestamptz not null default now()
);

alter table public.reports enable row level security;

drop policy if exists "reports_public_read" on public.reports;
create policy "reports_public_read"
on public.reports
for select
to anon, authenticated
using (true);

drop policy if exists "reports_open_insert" on public.reports;
create policy "reports_open_insert"
on public.reports
for insert
to anon, authenticated
with check (true);

drop policy if exists "reports_owner_update" on public.reports;
create policy "reports_owner_update"
on public.reports
for update
to authenticated
using (auth.uid() = owner_id)
with check (auth.uid() = owner_id);

drop policy if exists "reports_owner_delete" on public.reports;
create policy "reports_owner_delete"
on public.reports
for delete
to authenticated
using (auth.uid() = owner_id);

insert into storage.buckets (id, name, public)
values ('frp-files', 'frp-files', true)
on conflict (id) do nothing;

drop policy if exists "frp_files_public_read" on storage.objects;
create policy "frp_files_public_read"
on storage.objects
for select
to anon, authenticated
using (bucket_id = 'frp-files');

drop policy if exists "frp_files_open_insert" on storage.objects;
create policy "frp_files_open_insert"
on storage.objects
for insert
to anon, authenticated
with check (bucket_id = 'frp-files');

drop policy if exists "frp_files_owner_update" on storage.objects;
create policy "frp_files_owner_update"
on storage.objects
for update
to authenticated
using (bucket_id = 'frp-files' and owner = auth.uid())
with check (bucket_id = 'frp-files' and owner = auth.uid());

drop policy if exists "frp_files_owner_delete" on storage.objects;
create policy "frp_files_owner_delete"
on storage.objects
for delete
to authenticated
using (bucket_id = 'frp-files' and owner = auth.uid());
