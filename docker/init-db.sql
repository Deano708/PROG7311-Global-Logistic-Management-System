
-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'GLMS')
BEGIN
    CREATE DATABASE GLMS;
END
GO

USE GLMS;
GO

-- Roles
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles (
        RoleID   INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(75) NOT NULL UNIQUE
    );

    INSERT INTO Roles (RoleName) VALUES
        ('Admin'),
        ('Manager'),
        ('Viewer');
END
GO

-- Statuses
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Statuses')
BEGIN
    CREATE TABLE Statuses (
        StatusID    INT IDENTITY(1,1) PRIMARY KEY,
        StatusName  NVARCHAR(50) NOT NULL,
        Category    NVARCHAR(50) NOT NULL,
        Description NVARCHAR(250)
    );

    INSERT INTO Statuses (StatusName, Category, Description) VALUES
        ('Active',   'Contract',       'Contract is currently active'),
        ('On-Hold',  'Contract',       'Contract not yet started'),
        ('Expired',  'Contract',       'Contract end date has passed'),
        ('Pending',  'ServiceRequest', 'Request awaiting review'),
        ('Approved', 'ServiceRequest', 'Request has been approved'),
        ('Declined', 'ServiceRequest', 'Request has been declined');
END
GO

-- Clients
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Clients')
BEGIN
    CREATE TABLE Clients (
        ClientID    INT IDENTITY(1,1) PRIMARY KEY,
        Name        NVARCHAR(150) NOT NULL,
        ClientEmail NVARCHAR(250),
        Region      NVARCHAR(100),
        CreatedAt   DATETIME DEFAULT GETDATE()
    );
END
GO

-- Contracts
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Contracts')
BEGIN
    CREATE TABLE Contracts (
        ContractID              INT IDENTITY(1,1) PRIMARY KEY,
        StartDate               DATETIME NOT NULL,
        EndDate                 DATETIME NOT NULL,
        ServiceLevel            NVARCHAR(100),
        SignedAgreementFilePath NVARCHAR(350),
        CreatedAt               DATETIME DEFAULT GETDATE(),
        ClientID                INT NOT NULL,
        StatusID                INT NOT NULL,
        CONSTRAINT FK__Contracts__Clien__6B24EA82
            FOREIGN KEY (ClientID) REFERENCES Clients(ClientID),
        CONSTRAINT FK__Contracts__Statu__6C190EBB
            FOREIGN KEY (StatusID) REFERENCES Statuses(StatusID)
    );
END
GO

-- ServiceRequests
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ServiceRequests')
BEGIN
    CREATE TABLE ServiceRequests (
        ServiceRequestID INT IDENTITY(1,1) PRIMARY KEY,
        Description      NVARCHAR(325) NOT NULL,
        CostUSD          DECIMAL(18,2) NOT NULL,
        CostZAR          DECIMAL(18,2) NOT NULL,
        CreatedAt        DATETIME DEFAULT GETDATE(),
        ContractID       INT NOT NULL,
        StatusID         INT NOT NULL,
        CONSTRAINT FK__ServiceRe__Contr__6FE99F9F
            FOREIGN KEY (ContractID) REFERENCES Contracts(ContractID),
        CONSTRAINT FK__ServiceRe__Statu__70DDC3D8
            FOREIGN KEY (StatusID) REFERENCES Statuses(StatusID)
    );
END
GO

-- Users
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        UserID       INT IDENTITY(1,1) PRIMARY KEY,
        FullName     NVARCHAR(175) NOT NULL,
        Email        NVARCHAR(150) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(250) NOT NULL,
        IsActive     BIT DEFAULT 1,
        CreatedAt    DATETIME DEFAULT GETDATE(),
        RoleID       INT NOT NULL,
        CONSTRAINT FK__Users__RoleID__628FA481
            FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
    );
END
GO

PRINT 'GLMS database initialised successfully.';
GO