import React, { useState, useEffect } from 'react';
import { useToggleUserStatusMutation, useUsersQuery } from '../hooks/useQueries';
import { User } from '../types';
import { RoleBadge } from '../components/common/Badge';
import { EmployeeFormModal } from '../components/employees/EmployeeFormModal';
import { toast } from '../store/useToastStore';
import { useAuthStore } from '../store/useAuthStore';
import {
  UserPlus,
  Search,
  Edit2,
  Power,
  CheckCircle2,
  XCircle,
  Briefcase,
  Calendar,
} from 'lucide-react';

export const EmployeesPage: React.FC = () => {
  const { user: currentUser } = useAuthStore();
  const { data: users = [], isLoading, isError } = useUsersQuery();
  const toggleStatusMutation = useToggleUserStatusMutation();

  const [searchTerm, setSearchTerm] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [employeeToEdit, setEmployeeToEdit] = useState<User | null>(null);

  useEffect(() => {
    if (isError) {
      toast.error('Failed to load user directory');
    }
  }, [isError]);

  const handleToggleStatus = async (user: User) => {
    if (user.id === currentUser?.id) {
      toast.warning('You cannot deactivate your own account.');
      return;
    }

    try {
      await toggleStatusMutation.mutateAsync(user.id);
      const action = user.isActive ? 'deactivated' : 'activated';
      toast.success(`${user.fullName} has been ${action}`);
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Failed to update user status');
    }
  };

  const filteredUsers = users.filter((u) => {
    if (!searchTerm) return true;
    const term = searchTerm.toLowerCase();
    return (
      u.fullName.toLowerCase().includes(term) ||
      u.username.toLowerCase().includes(term) ||
      u.email.toLowerCase().includes(term) ||
      (u.department && u.department.toLowerCase().includes(term))
    );
  });

  return (
    <div className="w-full max-w-full min-w-0 space-y-4 sm:space-y-6 animate-in fade-in duration-300">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 sm:gap-4 min-w-0">
        <div className="min-w-0">
          <h1 className="text-lg sm:text-2xl font-bold tracking-tight text-slate-900">
            Employee Directory & Access Management
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Provision staff profiles, assign organizational roles, and manage activation privileges.
          </p>
        </div>

        <button
          onClick={() => {
            setEmployeeToEdit(null);
            setIsModalOpen(true);
          }}
          className="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 transition shadow-xs"
        >
          <UserPlus className="w-4 h-4" />
          <span>Add New Employee</span>
        </button>
      </div>

      {/* Filter / Search Bar */}
      <div className="p-3.5 sm:p-4 bg-white rounded-2xl border border-slate-200/80 shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-3 sm:gap-4 min-w-0">
        <div className="relative flex-1 w-full min-w-0 sm:max-w-md">
          <Search className="w-4 h-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Search by name, username, email, or department..."
            className="w-full pl-9 pr-4 py-2.5 sm:py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:outline-hidden focus:ring-1 focus:ring-blue-500 transition"
          />
        </div>

        <div className="text-xs text-slate-400 font-medium flex items-center justify-between sm:justify-end">
          <span>Total Staff:</span>
          <span className="text-slate-800 font-semibold ml-1.5">{filteredUsers.length}</span>
        </div>
      </div>

      {/* Employees Container */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden min-w-0 w-full">
        {/* 1. MOBILE & TABLET CARD LIST VIEW (< 1024px) */}
        <div className="lg:hidden divide-y divide-slate-100">
          {isLoading ? (
            <div className="py-12 text-center text-slate-400 text-xs">
              <div className="flex items-center justify-center gap-2">
                <span className="w-4 h-4 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
                <span>Loading employee directory...</span>
              </div>
            </div>
          ) : filteredUsers.length === 0 ? (
            <div className="py-12 px-4 text-center text-slate-400 text-xs">
              No employees found matching the search criteria.
            </div>
          ) : (
            filteredUsers.map((emp) => (
              <div key={emp.id} className="p-4 space-y-3 hover:bg-slate-50/60 transition">
                <div className="flex items-start justify-between gap-2 min-w-0">
                  <div className="flex items-center gap-3 flex-1 min-w-0">
                    <div className="w-10 h-10 rounded-full bg-blue-50 text-blue-700 font-bold flex items-center justify-center text-sm border border-blue-100 shrink-0">
                      {emp.fullName.charAt(0).toUpperCase()}
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="font-semibold text-sm text-slate-900 truncate">{emp.fullName}</div>
                      <div className="text-xs text-slate-400 truncate [overflow-wrap:anywhere]">
                        @{emp.username} &bull; {emp.email}
                      </div>
                    </div>
                  </div>
                  <div className="shrink-0">
                    <RoleBadge role={emp.roleName} size="sm" />
                  </div>
                </div>

                <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-slate-500 pt-1">
                  <div className="flex items-center gap-1.5">
                    <Briefcase className="w-3.5 h-3.5 text-slate-400" />
                    <span>{emp.department || 'No Department'}</span>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <Calendar className="w-3.5 h-3.5 text-slate-400" />
                    <span>
                      {new Date(emp.createdDate).toLocaleDateString('en-US', {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric',
                      })}
                    </span>
                  </div>
                </div>

                <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-slate-100">
                  <div>
                    {emp.isActive ? (
                      <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                        <CheckCircle2 className="w-3.5 h-3.5" />
                        <span>Active</span>
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-slate-100 text-slate-500 border border-slate-200">
                        <XCircle className="w-3.5 h-3.5" />
                        <span>Deactivated</span>
                      </span>
                    )}
                  </div>

                  <div className="flex flex-wrap items-center gap-2">
                    <button
                      onClick={() => {
                        setEmployeeToEdit(emp);
                        setIsModalOpen(true);
                      }}
                      className="min-h-[36px] inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium text-slate-700 bg-slate-100 hover:bg-blue-50 hover:text-blue-600 transition"
                    >
                      <Edit2 className="w-3.5 h-3.5" />
                      <span>Edit</span>
                    </button>

                    <button
                      onClick={() => handleToggleStatus(emp)}
                      disabled={emp.id === currentUser?.id}
                      className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium transition disabled:opacity-30 disabled:cursor-not-allowed ${
                        emp.isActive
                          ? 'bg-rose-50 text-rose-600 hover:bg-rose-100'
                          : 'bg-emerald-50 text-emerald-600 hover:bg-emerald-100'
                      }`}
                    >
                      <Power className="w-3.5 h-3.5" />
                      <span>{emp.isActive ? 'Deactivate' : 'Activate'}</span>
                    </button>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>

        {/* 2. DESKTOP TABLE VIEW (>= 1024px) */}
        <div className="hidden lg:block overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50/75 border-b border-slate-200 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">
                <th className="py-3.5 px-6">Staff Member</th>
                <th className="py-3.5 px-4">Role</th>
                <th className="py-3.5 px-4">Department</th>
                <th className="py-3.5 px-4">Status</th>
                <th className="py-3.5 px-4">Created Date</th>
                <th className="py-3.5 px-6 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-xs text-slate-700">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-slate-400">
                    <div className="flex items-center justify-center gap-2">
                      <span className="w-4 h-4 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
                      <span>Loading employee directory...</span>
                    </div>
                  </td>
                </tr>
              ) : filteredUsers.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-slate-400">
                    No employees found matching the search criteria.
                  </td>
                </tr>
              ) : (
                filteredUsers.map((emp) => (
                  <tr key={emp.id} className="hover:bg-slate-50/70 transition">
                    <td className="py-4 px-6">
                      <div className="flex items-center gap-3">
                        <div className="w-8 h-8 rounded-full bg-blue-50 text-blue-700 font-bold flex items-center justify-center text-xs border border-blue-100">
                          {emp.fullName.charAt(0).toUpperCase()}
                        </div>
                        <div>
                          <div className="font-semibold text-slate-800">{emp.fullName}</div>
                          <div className="text-[11px] text-slate-400">
                            @{emp.username} &bull; {emp.email}
                          </div>
                        </div>
                      </div>
                    </td>

                    <td className="py-4 px-4">
                      <RoleBadge role={emp.roleName} />
                    </td>

                    <td className="py-4 px-4 text-slate-600">
                      {emp.department ? (
                        <span className="flex items-center gap-1.5">
                          <Briefcase className="w-3.5 h-3.5 text-slate-400" />
                          <span>{emp.department}</span>
                        </span>
                      ) : (
                        <span className="text-slate-400 italic">None assigned</span>
                      )}
                    </td>

                    <td className="py-4 px-4">
                      {emp.isActive ? (
                        <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                          <CheckCircle2 className="w-3.5 h-3.5" />
                          <span>Active</span>
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-slate-100 text-slate-500 border border-slate-200">
                          <XCircle className="w-3.5 h-3.5" />
                          <span>Deactivated</span>
                        </span>
                      )}
                    </td>

                    <td className="py-4 px-4 text-slate-500 text-[11px]">
                      {new Date(emp.createdDate).toLocaleDateString('en-US', {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric',
                      })}
                    </td>

                    <td className="py-4 px-6 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => {
                            setEmployeeToEdit(emp);
                            setIsModalOpen(true);
                          }}
                          title="Edit Employee"
                          className="p-1.5 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>

                        <button
                          onClick={() => handleToggleStatus(emp)}
                          disabled={emp.id === currentUser?.id}
                          title={
                            emp.id === currentUser?.id
                              ? 'Cannot deactivate self'
                              : emp.isActive
                              ? 'Deactivate User'
                              : 'Activate User'
                          }
                          className={`p-1.5 rounded-lg transition disabled:opacity-30 disabled:cursor-not-allowed ${
                            emp.isActive
                              ? 'text-slate-400 hover:text-rose-600 hover:bg-rose-50'
                              : 'text-slate-400 hover:text-emerald-600 hover:bg-emerald-50'
                          }`}
                        >
                          <Power className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Employee Add/Edit Modal */}
      <EmployeeFormModal
        isOpen={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setEmployeeToEdit(null);
        }}
        employeeToEdit={employeeToEdit}
      />
    </div>
  );
};
