EXECUTE sp_rename N'[$schemaname$].ApiClaims', N'ApiResourceClaims', 'OBJECT'
GO
ALTER TABLE [$schemaname$].ApiResourceClaims SET (LOCK_ESCALATION = TABLE)
GO

EXECUTE sp_rename N'[$schemaname$].ApiProperties', N'ApiResourceProperties', 'OBJECT'
GO
ALTER TABLE [$schemaname$].ApiResourceProperties SET (LOCK_ESCALATION = TABLE)
GO

EXECUTE sp_rename N'[$schemaname$].ApiSecrets', N'ApiResourceSecrets', 'OBJECT'
GO
ALTER TABLE [$schemaname$].ApiResourceSecrets SET (LOCK_ESCALATION = TABLE)
GO

CREATE TABLE [$schemaname$].ApiResourceScopes
(
Id int NOT NULL IDENTITY (1, 1),
ApiResourceId int NOT NULL,
Scope nvarchar(200) NOT NULL
)  ON [PRIMARY]
GO
ALTER TABLE [$schemaname$].ApiResourceScopes ADD CONSTRAINT
PK_ApiResourceScopes PRIMARY KEY CLUSTERED
(
Id
) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
CREATE NONCLUSTERED INDEX IX_ApiResourceScopes ON [$schemaname$].ApiResourceScopes
(
ApiResourceId
) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [$schemaname$].ApiResourceScopes SET (LOCK_ESCALATION = TABLE)
GO

insert into [$schemaname$].[ApiResourceScopes]  (ApiResourceId, Scope) select [ApiResourceId], [Name] from [$schemaname$].ApiScopes


ALTER TABLE [$schemaname$].ApiScopes
DROP CONSTRAINT FK_ApiScopes_ApiResources_ApiResourceId
GO
ALTER TABLE [$schemaname$].ApiResources SET (LOCK_ESCALATION = TABLE)

GO
ALTER TABLE [$schemaname$].ApiScopes ADD
Enabled bit NOT NULL CONSTRAINT DF_ApiScopes_Enabled DEFAULT 1
GO
DROP INDEX IX_ApiScopes_ApiResourceId ON [$schemaname$].ApiScopes
GO
ALTER TABLE [$schemaname$].ApiScopes
DROP COLUMN ApiResourceId
GO
ALTER TABLE [$schemaname$].ApiScopes SET (LOCK_ESCALATION = TABLE)
GO

CREATE TABLE [$schemaname$].ApiScopeProperties
(
Id int NOT NULL IDENTITY (1, 1),
[Key] nvarchar(250) NOT NULL,
Value nvarchar(2000) NOT NULL,
ScopeId int NOT NULL
)  ON [PRIMARY]
GO
ALTER TABLE [$schemaname$].ApiScopeProperties ADD CONSTRAINT
PK_ApiScopeProperties PRIMARY KEY CLUSTERED
(
Id
) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
CREATE NONCLUSTERED INDEX IX_ApiScopeProperties ON [$schemaname$].ApiScopeProperties
(
ScopeId
) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [$schemaname$].ApiScopeProperties SET (LOCK_ESCALATION = TABLE)
GO

EXECUTE sp_rename N'[$schemaname$].IdentityClaims', N'IdentityResourceClaims', 'OBJECT'
GO
ALTER TABLE [$schemaname$].IdentityResourceClaims SET (LOCK_ESCALATION = TABLE)
GO

EXECUTE sp_rename N'[$schemaname$].IdentityProperties', N'IdentityResourceProperties', 'OBJECT'
GO
ALTER TABLE [$schemaname$].IdentityResourceProperties SET (LOCK_ESCALATION = TABLE)
GO


ALTER TABLE [$schemaname$].ApiResources ADD
AllowedAccessTokenSigningAlgorithms nvarchar(100) NULL,
ShowInDiscoveryDocument bit NOT NULL CONSTRAINT DF_ApiResources_ShowInDiscoveryDocument DEFAULT 1
GO

ALTER TABLE [$schemaname$].Clients ADD
AllowedIdentityTokenSigningAlgorithms nvarchar(100) NULL,
RequireRequestObject bit NOT NULL CONSTRAINT DF_Clients_RequireRequestObject DEFAULT 0
GO

INSERT INTO [$schemaname$].ApiScopes (Name,DisplayName,Description,Required,Emphasize,ShowInDiscoveryDocument,Enabled) VALUES 
('openid','openid',NULL,0,0,1,1)
,('profile','profile',NULL,0,0,1,1);

delete from [$schemaname$].IdentityResources;