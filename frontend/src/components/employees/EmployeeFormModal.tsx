import React, { useState, useEffect } from 'react';
import { User, Role, UserCreatePayload, UserUpdatePayload } from '../../types';
import { userService } from '../../services/userService';
import { Modal } from '../common/Modal';
import { toast } from '../../store/useToastStore';

interface EmployeeFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  employeeToEdit?: User | null;
  onSuccess: () => void;
}

export const EmployeeFormModal: React.FC<EmployeeFormModalProps> = ({
  isOpen,
  onClose,
  employeeToEdit,
  onSuccess,
}) => {
  const [roles, setRoles] = useState<Role[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [formData, setFormData] = useState({
    username: '',
    email: '',
    fullName: '',
    department: '',
    password: '',
    roleId: 2, // Default to Employee role
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (isOpen) {
      loadRoles();
      if (employeeToEdit) {
        setFormData({
          username: employeeToEdit.username,
          email: employeeToEdit.email,
          fullName: employeeToEdit.fullName,
          department: employeeToEdit.department || '',
          password: '',
          roleId: employeeToEdit.roleId,
        });
      } else {
        setFormData({
          username: '',
          email: '',
          fullName: '',
          department: '',
          password: '',
          roleId: 2,
        });
      }
      setErrors({});
    }
  }, [isOpen, employeeToEdit]);

  const loadRoles = async () => {
    try {
      const data = await userService.getRoles();
      setRoles(data);
    } catch {
      // Fallback roles if endpoint is restricted
      setRoles([
        { id: 1, name: 'Admin' },
        { id: 2, name: 'Employee' },
      ]);
    }
  };

  const validate = () => {
    const errs: Record<string, string> = {};
    if (!employeeToEdit && !formData.username.trim()) errs.username = 'Username is required';
    if (!formData.fullName.trim()) errs.fullName = 'Full Name is required';
    if (!formData.email.trim() || !formData.email.includes('@')) {
      errs.email = 'A valid email is required';
    }
    if (!employeeToEdit && (!formData.password || formData.password.length < 6)) {
      errs.password = 'Password must be at least 6 characters';
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    setIsSubmitting(true);
    try {
      if (employeeToEdit) {
        const payload: UserUpdatePayload = {
          id: employeeToEdit.id,
          fullName: formData.fullName.trim(),
          email: formData.email.trim(),
          department: formData.department.trim(),
          roleId: formData.roleId,
          password: formData.password ? formData.password : undefined,
        };
        await userService.updateUser(employeeToEdit.id, payload);
        toast.success('Employee updated successfully');
      } else {
        const payload: UserCreatePayload = {
          username: formData.username.trim(),
          fullName: formData.fullName.trim(),
          email: formData.email.trim(),
          department: formData.department.trim(),
          password: formData.password,
          roleId: formData.roleId,
        };
        await userService.createUser(payload);
        toast.success('Employee account created successfully');
      }
      onSuccess();
      onClose();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Operation failed');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={employeeToEdit ? 'Edit Employee' : 'Add New Employee'}
      subtitle={
        employeeToEdit
          ? `Editing ${employeeToEdit.fullName}`
          : 'Create a new employee profile and credentials'
      }
      maxWidth="md"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Full Name */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1">
            Full Name <span className="text-rose-500">*</span>
          </label>
          <input
            type="text"
            value={formData.fullName}
            onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
            placeholder="e.g. Michael Scott"
            className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
          />
          {errors.fullName && <p className="text-[11px] text-rose-500 mt-1">{errors.fullName}</p>}
        </div>

        {/* Username (only editable on create) */}
        {!employeeToEdit && (
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Username <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              value={formData.username}
              onChange={(e) => setFormData({ ...formData, username: e.target.value })}
              placeholder="e.g. michael.scott"
              className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
            />
            {errors.username && <p className="text-[11px] text-rose-500 mt-1">{errors.username}</p>}
          </div>
        )}

        {/* Email */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1">
            Email Address <span className="text-rose-500">*</span>
          </label>
          <input
            type="email"
            value={formData.email}
            onChange={(e) => setFormData({ ...formData, email: e.target.value })}
            placeholder="e.g. michael.scott@taskflow.com"
            className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
          />
          {errors.email && <p className="text-[11px] text-rose-500 mt-1">{errors.email}</p>}
        </div>

        {/* Department & Role */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Department</label>
            <input
              type="text"
              value={formData.department}
              onChange={(e) => setFormData({ ...formData, department: e.target.value })}
              placeholder="e.g. Engineering, Sales, QA"
              className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Role</label>
            <select
              value={formData.roleId}
              onChange={(e) => setFormData({ ...formData, roleId: Number(e.target.value) })}
              className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
            >
              {roles.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Password */}
        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1">
            {employeeToEdit ? 'Reset Password (optional)' : 'Password'} {!employeeToEdit && <span className="text-rose-500">*</span>}
          </label>
          <input
            type="password"
            value={formData.password}
            onChange={(e) => setFormData({ ...formData, password: e.target.value })}
            placeholder={employeeToEdit ? 'Leave blank to keep current password' : 'At least 6 characters'}
            className="w-full text-xs px-3.5 py-2.5 rounded-xl border border-slate-300 focus:outline-hidden focus:ring-1 focus:ring-indigo-500 bg-white"
          />
          {errors.password && <p className="text-[11px] text-rose-500 mt-1">{errors.password}</p>}
        </div>

        {/* Action Buttons */}
        <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 rounded-xl text-xs font-medium text-slate-600 hover:bg-slate-100 transition"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="px-5 py-2 rounded-xl text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 transition shadow-xs"
          >
            {isSubmitting ? 'Saving...' : employeeToEdit ? 'Save Changes' : 'Create Account'}
          </button>
        </div>
      </form>
    </Modal>
  );
};
