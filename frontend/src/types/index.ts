export interface User {
  id: number;
  username: string;
  email: string;
  fullName: string;
  department?: string;
  roleId: number;
  roleName: 'Admin' | 'Employee' | string;
  isActive: boolean;
  createdDate: string;
  modifiedDate?: string;
}

export interface Role {
  id: number;
  name: string;
  description?: string;
}

export interface TaskItem {
  id: number;
  title: string;
  description?: string;
  priority: 'Low' | 'Medium' | 'High';
  status: 'New' | 'In Progress' | 'Completed';
  startDate: string;
  dueDate: string;
  assignedToUserId: number;
  assignedToUserName: string;
  assignedToFullName: string;
  assignedToDepartment?: string;
  createdByUserId: number;
  createdByUserName: string;
  createdByFullName: string;
  createdDate: string;
  modifiedDate?: string;
  isOverdue: boolean;
  commentsCount: number;
}

export interface TaskComment {
  id: number;
  taskId: number;
  userId: number;
  userName: string;
  userFullName: string;
  userRole: string;
  comment: string;
  createdDate: string;
  modifiedDate?: string;
}

export interface MetricCount {
  label: string;
  count: number;
  percentage: number;
}

export interface EmployeeWorkload {
  userId: number;
  fullName: string;
  department: string;
  assignedTasksCount: number;
  completedTasksCount: number;
  pendingTasksCount: number;
  overdueTasksCount: number;
}

export interface DashboardSummary {
  totalTasks: number;
  newTasks: number;
  inProgressTasks: number;
  completedTasks: number;
  overdueTasks: number;
  totalEmployees: number;
  activeEmployees: number;
  priorityBreakdown: MetricCount[];
  statusBreakdown: MetricCount[];
  employeeWorkloads: EmployeeWorkload[];
  recentTasks: TaskItem[];
  overdueTaskList: TaskItem[];
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
  statusCode: number;
}

export interface LoginRequest {
  usernameOrEmail: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface TaskQueryParams {
  search?: string;
  assignedToUserId?: number;
  status?: string;
  priority?: string;
  sortBy?: string;
  sortDescending?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export interface TaskCreatePayload {
  title: string;
  description?: string;
  priority: string;
  status: string;
  startDate: string;
  dueDate: string;
  assignedToUserId: number;
}

export interface TaskUpdatePayload {
  id: number;
  title: string;
  description?: string;
  priority: string;
  status: string;
  startDate: string;
  dueDate: string;
  assignedToUserId: number;
}

export interface UserCreatePayload {
  username: string;
  email: string;
  password: string;
  fullName: string;
  department?: string;
  roleId: number;
}

export interface UserUpdatePayload {
  id: number;
  email: string;
  fullName: string;
  department?: string;
  roleId: number;
  password?: string;
  isActive?: boolean;
}
