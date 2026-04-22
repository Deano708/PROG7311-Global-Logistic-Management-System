<h1 align="center">GLMS</h1>
<br>


Initial Class Diagram Design
<img width="811" height="587" alt="CD1 drawio" src="https://github.com/user-attachments/assets/e41ee7f6-2539-4fe2-a7a7-d2eb1be00732" />
<br>

New Updated Class Diagram
<img width="701" height="965" alt="CD New drawio" src="https://github.com/user-attachments/assets/9d9629d6-dbb4-4b30-9019-c7d20a5e7860" />
<br>


Unit Tests
<img width="982" height="488" alt="UnitTests" src="https://github.com/user-attachments/assets/f5b4aba6-8120-4846-9e43-b84ad1e12e2b" />

<br>

SSMS SQL Script:

Create Database GLMS;

--Roles Table 
Create Table Roles(
RoleID int Primary Key Identity(1,1),
RoleName Nvarchar(75) not null Unique
);

--Role Insertion future proofing
Insert into Roles (RoleName) Values
('Operations Manager'),
('Logistics Coordinator'),
('Compliance Officer'),
('Admin');

--Users Table 
Create Table Users(
UserID Int Primary Key Identity(1,1),
FullName Nvarchar(175) Not Null,
Email Nvarchar(150) Not Null Unique,
PasswordHash Nvarchar(250) Not Null,
IsActive Bit Default 1,       -- 1 = Active / 0 = Inactive
CreatedAt DateTime Default GetDate(),
RoleID int not Null Foreign Key References Roles(RoleID),
);


 --Clients Table 
Create Table Clients(
    ClientID Int Primary Key Identity(1,1),
    Name Nvarchar(150) Not Null,
    ClientEmail Nvarchar(250),
    Region Nvarchar(100),
    CreatedAt DateTime Default GetDate()
);

--Statuses Table
CREATE TABLE Statuses (
 StatusID INT PRIMARY KEY IDENTITY(1,1),
 StatusName NVARCHAR(50) NOT NULL,
 Category NVARCHAR(50) NOT NULL, -- e.g., 'Contract', 'ServiceRequest',
 [Description] NVARCHAR(250) 
);



--Contracts Table 
Create Table Contracts(
    ContractID Int Primary Key Identity(1,1),
    StartDate DateTime Not Null,
    EndDate DateTime Not Null,
    ServiceLevel Nvarchar(100),
    SignedAgreementFilePath Nvarchar(350),
    CreatedAt DateTime Default GetDate(),
    ClientID int not Null Foreign Key References Clients(ClientID),
    StatusID Int Not Null FOREIGN KEY REFERENCES Statuses(StatusID),    -- links to statuses table
);


--Service Requests Table 
Create Table  ServiceRequests(
    ServiceRequestID Int Primary Key Identity(1,1),
    Description Nvarchar(325) Not Null,
    CostUSD Decimal(18,2) Not Null,
    CostZAR Decimal(18,2) Not Null,
    CreatedAt DateTime Default GetDate(),
    ContractID int not Null Foreign Key References Contracts(ContractID),
    StatusID Int Not Null FOREIGN KEY REFERENCES Statuses(StatusID),    -- links to statuses table
);



Alter Table Contracts Alter Column StartDate DateTime2  Not Null;
Alter Table Contracts Alter Column EndDate DateTime2  Not Null;


--Insert New Clients
INSERT INTO Clients (Name, ClientEmail, Region)
VALUES 
('Warden Global Freight', 'logistics@wardenglobal.co.za', 'South Africa'),
('Homestead Logistics Ltd', 'operations@homesteadlogistics.co.za', 'South Africa'),
('Star Nav Maritime', 'contact@starnav.sg', 'Asia-Pacific');


--Insert Status Types
INSERT INTO Statuses (StatusName, Category, [Description])
VALUES 
-- Contract Category
('Active', 'Contract', 'Contract is valid and requests can be raised.'),
('Expired', 'Contract', 'Contract has reached its end date.'),
('On-Hold', 'Contract', 'Contract is temporarily suspended.'),

-- ServiceRequest Category (Full Workflow)
('Pending', 'ServiceRequest', 'Request is awaiting initial approval.'),
('Approved', 'ServiceRequest', 'Cost and validity verified; ready for execution.'),
('In Progress', 'ServiceRequest', 'Logistics operations and transport currently underway.'),
('On Hold', 'ServiceRequest', 'Request paused due to documentation or customs delays.'),
('Completed', 'ServiceRequest', 'Service fulfilled, delivered, and finalized.'),
('Rejected', 'ServiceRequest', 'Request denied due to cost, compliance, or inactive contract.'),
('Cancelled', 'ServiceRequest', 'Request withdrawn by the logistics coordinator.');



Declaration of AI Usage
<img width="618" height="743" alt="References" src="https://github.com/user-attachments/assets/969f30ec-6483-4244-b052-7756ae4d943f" />


