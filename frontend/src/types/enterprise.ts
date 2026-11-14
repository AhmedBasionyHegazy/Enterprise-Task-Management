export interface SessionInfo {
  id: string;
  deviceName?: string | null;
  ipAddress?: string | null;
  sessionStartedUtc: string;
  lastUsedUtc?: string | null;
  expiresUtc: string;
  isCurrent: boolean;
}

export interface AuditChange {
  field: string;
  oldValue?: string | null;
  newValue?: string | null;
  oldDisplay?: string | null;
  newDisplay?: string | null;
}

export interface AuditLogEntry {
  id: number;
  timestampUtc: string;
  userId?: number | null;
  username?: string | null;
  userFullName?: string | null;
  action: string;
  entityName: string;
  recordId?: string | null;
  relatedTaskId?: number | null;
  isSuccess: boolean;
  description?: string | null;
  changes: AuditChange[];
  ipAddress?: string | null;
  userAgent?: string | null;
  correlationId?: string | null;
}

export interface AuditQueryParams {
  userId?: number;
  username?: string;
  action?: string;
  entityName?: string;
  recordId?: string;
  success?: boolean;
  from?: string;
  to?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface NotificationItem {
  id: number;
  type: string;
  category: string;
  title: string;
  message: string;
  entityName?: string | null;
  entityId?: number | null;
  link?: string | null;
  actorName?: string | null;
  isRead: boolean;
  createdUtc: string;
}

export interface NotificationPreference {
  category: string;
  inAppEnabled: boolean;
}

export interface NotificationQueryParams {
  unreadOnly?: boolean;
  category?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface SystemLogEntry {
  timestampUtc: string;
  level: string;
  category: string;
  message: string;
  correlationId?: string | null;
  traceId?: string | null;
  clientIp?: string | null;
  userId?: string | null;
  username?: string | null;
  taskId?: string | null;
  action?: string | null;
  method?: string | null;
  path?: string | null;
  statusCode?: number | null;
  elapsedMs?: number | null;
  exceptionType?: string | null;
  exceptionMessage?: string | null;
}

export interface SystemLogQueryParams {
  level?: string;
  userId?: number;
  taskId?: number;
  action?: string;
  correlationId?: string;
  search?: string;
  fromUtc?: string;
  toUtc?: string;
  pageNumber?: number;
  pageSize?: number;
}
