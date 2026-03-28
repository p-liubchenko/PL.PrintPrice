import { Component, OnInit } from '@angular/core';
import { RolesService, RoleDto } from '../../services/roles.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-roles',
  standalone: false,
  templateUrl: './roles.component.html',
  styleUrls: ['./roles.component.css'],
})
export class RolesComponent implements OnInit {
  roles: RoleDto[] = [];
  allPermissions: string[] = [];
  loading = true;
  error = '';
  success = '';

  // Create form
  showCreateForm = false;
  createName = '';
  createPermissions: Set<string> = new Set();

  // Edit form
  editingRole: RoleDto | null = null;
  editPermissions: Set<string> = new Set();

  constructor(
    private rolesService: RolesService,
    public auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.rolesService.getAllPermissions().subscribe({
      next: perms => {
        this.allPermissions = perms;
        this.load();
      },
      error: () => { this.loading = false; this.error = 'Failed to load permissions.'; },
    });
  }

  load(): void {
    this.loading = true;
    this.rolesService.getAll().subscribe({
      next: roles => { this.roles = roles; this.loading = false; },
      error: () => { this.loading = false; },
    });
  }

  // ── Permission groups for display ─────────────────────────────────────

  get permissionGroups(): { prefix: string; permissions: string[] }[] {
    const map = new Map<string, string[]>();
    for (const p of this.allPermissions) {
      const prefix = p.split('.')[0];
      if (!map.has(prefix)) map.set(prefix, []);
      map.get(prefix)!.push(p);
    }
    return Array.from(map.entries()).map(([prefix, permissions]) => ({ prefix, permissions }));
  }

  // ── Create ────────────────────────────────────────────────────────────

  openCreate(): void {
    this.showCreateForm = true;
    this.editingRole = null;
    this.createName = '';
    this.createPermissions = new Set();
    this.error = '';
    this.success = '';
  }

  toggleCreatePermission(p: string): void {
    if (this.createPermissions.has(p)) this.createPermissions.delete(p);
    else this.createPermissions.add(p);
  }

  create(): void {
    this.error = '';
    this.rolesService.create(this.createName, Array.from(this.createPermissions)).subscribe({
      next: role => {
        this.roles.push(role);
        this.showCreateForm = false;
        this.success = `Role "${role.name}" created.`;
      },
      error: (err: any) => {
        this.error = Array.isArray(err?.error) ? err.error.join(' ') : (err?.error ?? 'Failed to create role.');
      },
    });
  }

  // ── Edit ──────────────────────────────────────────────────────────────

  openEdit(role: RoleDto): void {
    this.editingRole = role;
    this.showCreateForm = false;
    this.editPermissions = new Set(role.permissions);
    this.error = '';
    this.success = '';
  }

  toggleEditPermission(p: string): void {
    if (this.editPermissions.has(p)) this.editPermissions.delete(p);
    else this.editPermissions.add(p);
  }

  saveEdit(): void {
    if (!this.editingRole) return;
    this.error = '';
    this.rolesService.update(this.editingRole.id, Array.from(this.editPermissions)).subscribe({
      next: () => {
        this.editingRole!.permissions = Array.from(this.editPermissions).sort();
        this.success = `Role "${this.editingRole!.name}" updated.`;
        this.editingRole = null;
      },
      error: (err: any) => {
        this.error = err?.error ?? 'Failed to update role.';
      },
    });
  }

  // ── Delete ────────────────────────────────────────────────────────────

  delete(role: RoleDto): void {
    if (!confirm(`Delete role "${role.name}"? Users currently in this role will lose its permissions.`)) return;
    this.error = '';
    this.rolesService.delete(role.id).subscribe({
      next: () => {
        this.roles = this.roles.filter(r => r.id !== role.id);
        this.success = `Role "${role.name}" deleted.`;
        if (this.editingRole?.id === role.id) this.editingRole = null;
      },
      error: (err: any) => {
        this.error = err?.error ?? 'Failed to delete role.';
      },
    });
  }
}
