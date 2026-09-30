Objects / Models

IdentityUser
	Phone
	Email

Property
	PropertyName

Unit
	PropertyId
	UnitNumber
	Bedrooms
	MonthlyRent
	UnitTypeId
	
UnitType
	UnitTypeName
	IsActive
	

Lease
	UnitID
	ApplicationId
	StartDate
	EndDate

Application
	UnitID
	CreatedAt
	ApplicationStatusId
	ManagerNotes
	RowVersion  (byte[] / Timestamp)  - EF Core optimistic locking

Applicant
	UserID
	FirstName
	LastName
	MiddleName
	CurrentResidenceId

ApplicationApplicant
	ApplicationID
	ApplicantID

ResidenceHistory
	ApplicationApplicantId
	Street
	City
	State
	Zip
	LandlordName
	LandlordPhone
	MoveInDate
	MoveOutDate

ApplicationStatus
	StatusName:
		Draft
		Submitted
		Returned
		Approved
		Denied
		Withdrawn
		Leased
		Under Review

ApplicationReview
	ReviewUserId
	ApplicationId
	ReviewDate
	OutcomeStatusId
	Comment

AuditActionType
	Submission
	StartReview
	CompleteReview
	AddProperty
	EditProperty
	RemoveProperty
	AddApplication
	etc...

AuditActionHistory
	ActionTypeId
	UserID
	Timestamp
	EntityName
	EntityId
	FromStatusId
	ToStatusId
	OldObjectValue - json
	NewObjectValue - json
	ManagerNoteId
	

ManagerNote
	EntityName
	EntityId
	UserId
	NoteText
	CreatedAt