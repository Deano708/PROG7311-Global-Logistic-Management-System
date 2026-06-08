<h1 align="center">GLMS Summative</h1>
<br>

<h4 align="center">YouTube Link: https://youtu.be/EK00ZQA5l40 </h4>



<br>

<h4 align="center">DB Class Diagram</h4>
<img width="701" height="965" alt="CD New drawio" src="https://github.com/user-attachments/assets/9d9629d6-dbb4-4b30-9019-c7d20a5e7860" />
(Draw.io, 2026)
<br>
<br>

<h4 align="center">Unit Tests</h4>
<img width="1116" height="982" alt="Screenshot 2026-06-08 165527" src="https://github.com/user-attachments/assets/dc2c84aa-a817-4d47-a917-269bcc5b25d9" />

<br>
<br>
<h4 align="center">Dockerization </h4>
<img width="1520" height="210" alt="Screenshot 2026-06-08 162510" src="https://github.com/user-attachments/assets/37977868-ee90-4f5e-8f32-7c4878455cd0" />

<br>
<br>
<h4 align="center">SSMS SQL Script:</h4>

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


<br>
<h4 align="center">References:</h4>
Draw.io,2026.CD1.drawio. Available at: <https://app.diagrams.net/> [Accessed 22 April 2026].
<br>
Draw.io,2026.CD New.drawio. Available at: <https://app.diagrams.net/> [Accessed 20 April 2026].
<br>
<h4 align="center">Declaration of AI Usage in my assessment:</h4>
<h4 align="center">Section, glmscontext, ContractController, FacadeServices, ServiceRequestController</h4>
<h4 align="center">AI Tool: Claude Sonnet 4,6 , Gemini</h4>
<h4 align="center">Purpose: Assist in scaffolding, designing and error handling parts of the GLMS</h4>
<h4 align="center">Date: 03/06/26 to 08/06/26</h4>
<h4 align="center">https://claude.ai/share/503d645e-0ce0-4796-920e-6e73ce7ccfb5</h4>
 <h4 align="center">https://gemini.google.com/share/b712e62df337</h4>
<img width="487" height="637" alt="Ref 1" src="https://github.com/user-attachments/assets/e2fc0f34-8e42-47b3-bf3a-fb64a618ef4d" />
<img width="482" height="422" alt="Ref 2" src="https://github.com/user-attachments/assets/14235e49-e03c-4b6a-87c6-fb7eefce9853" />


