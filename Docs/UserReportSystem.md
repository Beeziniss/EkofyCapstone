# User Report System - H? th?ng báo cáo vi ph?m ng??i dùng

## T?ng quan
H? th?ng cho phép user báo cáo các vi ph?m chính sách, moderator x? lý và áp d?ng các hành ??ng ph?t t??ng ?ng.

## Ki?n trúc

### Entities
1. **UserReport**: L?u tr? thông tin báo cáo vi ph?m
2. **UserRestriction**: L?u l?ch s? các hành ??ng x? ph?t

### Enums
- **ReportStatus**: Tr?ng thái x? lý (Pending, UnderReview, Approved, Rejected, Dismissed, Escalated)
- **ReportType**: Lo?i vi ph?m (Spam, Harassment, HateSpeech, InappropriateContent, ...)
- **ReportAction**: Hành ??ng x? ph?t (NoAction, Warning, ContentRemoval, TemporarySuspension, PermanentBan, ...)
- **ReportPriority**: M?c ?? ?u tiên (Low, Medium, High, Critical)

## Workflow

### 1. User báo cáo vi ph?m
```graphql
mutation CreateReport {
  createReportAsync(request: {
    reportedUserId: "USER_ID"
    reportType: Spam
    description: "Chi ti?t vi ph?m"
    relatedContentId: "CONTENT_ID"
    relatedContentType: "Track"
    evidenceUrls: ["url1", "url2"]
  }) {
    id
    status
    priority
  }
}
```

**Quy trình:**
- User không th? t? báo cáo chính mình
- Không ???c báo cáo l?p l?i trong vòng 24h
- Priority t? ??ng t?ng d?a trên s? l?n user b? báo cáo:
  - 0-1 reports: Low
  - 2-4 reports: Medium
  - 5-9 reports: High
  - 10+ reports: Critical

### 2. Moderator xem danh sách báo cáo
```graphql
query GetReports {
  getReportsAsync(request: {
    status: Pending
    priority: High
    pageNumber: 1
    pageSize: 20
  }) {
    reports {
      id
      reportedUserName
      reportType
      description
      status
      priority
      createdAt
    }
    totalCount
    hasNextPage
  }
}
```

### 3. Assign báo cáo cho moderator
```graphql
mutation AssignReport {
  assignReportToModeratorAsync(
    reportId: "REPORT_ID"
    moderatorId: "MODERATOR_ID"
  )
}
```

### 4. Moderator x? lý báo cáo
```graphql
mutation ProcessReport {
  processReportAsync(request: {
    reportId: "REPORT_ID"
    status: Approved
    actionTaken: TemporarySuspension
    suspensionDays: 7
    moderatorNotes: "Vi ph?m chính sách v? spam"
  }) {
    id
    status
    actionTaken
    resolvedAt
  }
}
```

**Các hành ??ng có th? th?c hi?n:**
- **NoAction**: Không có vi ph?m
- **Warning**: C?nh báo user
- **ContentRemoval**: Xóa n?i dung vi ph?m
- **TemporarySuspension**: ?ình ch? t?m th?i (c?n `suspensionDays`)
- **PermanentBan**: C?m v?nh vi?n
- **AccountRestriction**: H?n ch? tính n?ng

### 5. Escalate báo cáo nghiêm tr?ng
```graphql
mutation EscalateReport {
  escalateReportAsync(reportId: "REPORT_ID")
}
```
Chuy?n priority lên Critical và status thành Escalated ?? admin x? lý.

### 6. Xem th?ng kê
```graphql
query GetReportStatistics {
  getReportStatisticsAsync {
    totalReports
    pendingReports
    underReviewReports
    resolvedReports
    reportsByType
    reportsByPriority
    topReportedUsers {
      userId
      userName
      reportCount
    }
  }
}
```

## X? ph?t User

### Temporary Suspension (?ình ch? t?m th?i)
- User status ? `Suspended`
- T?o `UserRestriction` record v?i `EndDate`
- Background job t? ??ng reactivate user khi h?t h?n
- Job ch?y m?i gi?: `check-expired-restrictions`

### Permanent Ban (C?m v?nh vi?n)
- User status ? `Banned`
- T?o `UserRestriction` record v?i `EndDate = null`
- User không th? login l?i

### Account Restriction (H?n ch? tính n?ng)
- T?o `UserRestriction` record v?i metadata ch? ??nh tính n?ng b? h?n ch?
- Có th? có ho?c không có th?i h?n

## Background Jobs

### RestrictionExpirationJob
- **Frequency**: M?i gi?
- **Function**: 
  - Tìm t?t c? restrictions ?ã h?t h?n (`EndDate <= now`)
  - Deactivate restriction
  - N?u user không còn restriction nào khác, reactivate user (status ? Active)

## Permissions

### User (Any authenticated user)
- T?o báo cáo vi ph?m
- Xem báo cáo c?a chính mình

### Moderator
- Xem t?t c? báo cáo
- Assign báo cáo cho chính mình
- X? lý báo cáo (approve/reject)
- Update priority
- Escalate báo cáo
- Xem statistics

### Admin
- T?t c? quy?n c?a Moderator
- Assign báo cáo cho b?t k? moderator nào
- Xóa báo cáo

## Validation Rules

### CreateReportRequest
- `reportedUserId`: Required, 24 characters (MongoDB ObjectId)
- `reportType`: Required, must be valid enum
- `description`: Required, 10-1000 characters
- `evidenceUrls`: Max 5 URLs

### ProcessReportRequest
- `reportId`: Required, 24 characters
- `status`: Required, valid enum
- `actionTaken`: Required, valid enum
- `suspensionDays`: 1-365 days (required if TemporarySuspension)
- `moderatorNotes`: Max 2000 characters

## Indexes (Recommended)

```javascript
// UserReport collection
db.UserReport.createIndex({ "ReportedUserId": 1, "Status": 1 })
db.UserReport.createIndex({ "ReporterId": 1, "CreatedAt": -1 })
db.UserReport.createIndex({ "Status": 1, "Priority": -1, "CreatedAt": -1 })
db.UserReport.createIndex({ "AssignedModeratorId": 1, "Status": 1 })

// UserRestriction collection
db.UserRestriction.createIndex({ "UserId": 1, "IsActive": 1 })
db.UserRestriction.createIndex({ "EndDate": 1, "IsActive": 1 })
db.UserRestriction.createIndex({ "ReportId": 1 })
```

## Error Handling

- `UnauthorizedCustomException`: Session expired ho?c không có token
- `ForbiddenCustomException`: Không có quy?n th?c hi?n action
- `NotFoundCustomException`: Report/User không t?n t?i
- `BadRequestCustomException`: Validation errors ho?c business logic errors

## Best Practices

1. **Không spam reports**: Gi?i h?n 1 report/user trong 24h
2. **Priority t? ??ng**: H? th?ng t? ??ng t?ng priority d?a trên l?ch s? vi ph?m
3. **Evidence**: Khuy?n khích ?ính kèm b?ng ch?ng (screenshots, URLs)
4. **Moderator notes**: Luôn ghi chú lý do khi x? lý báo cáo
5. **Escalation**: Báo cáo nghiêm tr?ng nên escalate lên admin
6. **Transaction**: X? lý báo cáo s? d?ng transaction ?? ??m b?o tính nh?t quán

## Future Enhancements

1. Email notification khi user b? suspend/ban
2. Appeal system cho user kháng cáo
3. Auto-ban khi ??t threshold s? reports
4. Report history tracking
5. Moderator performance metrics
6. Content filtering AI integration
