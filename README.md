# EmailSenderLibrary-SqlServer

A robust, efficient, and flexible SMTP email sender library for Microsoft SQL Server using .NET CLR integration. This project is designed to work with **all SQL Server versions that support CLR**—from legacy to modern—providing a secure and reliable alternative for sending emails directly from T-SQL.

---

## **Why EmailSenderLibrary?**

- **Universal Compatibility:**  
  Works seamlessly with both legacy and modern SQL Server instances (2005+), wherever CLR is supported.
- **Superior Efficiency:**  
  Delivers significantly better performance and reliability compared to built-in SQL Server email mechanisms.
- **Robust Error Handling:**  
  Handles errors gracefully, with clear feedback, strict parameter validation, and transparent reporting.
- **Flexible Integration:**  
  Supports full SMTP configuration, secure authentication, HTML emails, configurable timeouts, and use as a table-valued SQL function.
- **Maintainable & Extensible:**  
  Written in C#, fully versionable, and easy to update or adapt to new requirements.

---

## **Installation Instructions**

### 1. Compile the Code

Open a terminal in the root of your project folder and run:

```sh
dotnet clean
dotnet restore
dotnet build -c Release -v detailed > build.log
```

---

### 2. Merge All DLLs into a Single Assembly

Use ILRepack to merge dependencies into a single DLL:

```sh
C:\Users\{your_user}\.nuget\packages\ilrepack\2.0.34\tools\ILRepack.exe /out:C:\Users\{your_user}\source\repos\EmailSenderLibrary\bin\Release\EmailSenderLibrary.dll C:\Users\{your_user}\source\repos\EmailSenderLibrary\bin\Release\EmailSenderLibrary.dll /internalize /verbose
```

_(Replace `{your_user}` with your Windows username and adjust the paths as needed)_

---

### 3. Generate the Hash for SQL Server Assembly Signing

Run the following PowerShell commands:

```powershell
# Path to the DLL
$dllPath = "C:\Users\{your_user}\source\repos\EmailSenderLibrary\bin\Release\EmailSenderLibrary.dll"

# Calculate SHA512 hash
$hash = Get-FileHash -Path $dllPath -Algorithm SHA512

# Convert the hash to a binary string in the format 0xA1B2C3...
$binaryHex = "0x" + ($hash.Hash -split "(..)" | Where-Object { $_ } | ForEach-Object { $_ }) -join ""

# Output the hash
Write-Host ($binaryHex -replace " ", "")
```

Copy the generated hash for use in the next step.

---

### 4. Register the Assembly in SQL Server

In SQL Server Management Studio (SSMS), connect to the `master` database and execute:

```sql
EXEC sp_add_trusted_assembly
    @hash = {paste_the_generated_hash_here},
    @description = N'EmailSenderLibrary for SMTP Email sending';
```

Register the assembly (update the path if needed):

```sql
CREATE ASSEMBLY EmailSenderLibrary
FROM 'C:\Users\{your_user}\source\repos\EmailSenderLibrary\bin\Release\EmailSenderLibrary.dll'
WITH PERMISSION_SET = UNSAFE;
```

---

### 5. Create the Table-Valued Function

```sql
CREATE FUNCTION dbo.EmailSender
(
    @smtpHost NVARCHAR(4000),
    @smtpPort INT,
    @smtpUser NVARCHAR(4000),
    @smtpPass NVARCHAR(4000),
    @from NVARCHAR(4000),
    @to NVARCHAR(4000),
    @subject NVARCHAR(4000),
    @body NVARCHAR(MAX),
    @enableSsl BIT,
    @timeout INT
)
RETURNS TABLE
(
    success BIT,
    message NVARCHAR(MAX),
    timing BIGINT,
    exceptionType NVARCHAR(MAX)
)
AS
EXTERNAL NAME EmailSenderLibrary.[EmailSenderLibrary.EmailSender].FrisiaSendMail;
```

---

## **Usage Example**

```sql
SELECT * FROM dbo.EmailSender(
    'smtp.office365.com',               -- @smtpHost
    587,                                -- @smtpPort
    'user@company.com.br',              -- @smtpUser
    'appPasswordOrPassword',            -- @smtpPass
    'System <system@company.com.br>',   -- @from
    'recipient@domain.com',             -- @to
    'Subject via CLR',                  -- @subject
    'Email body (HTML allowed)',        -- @body
    1,                                  -- @enableSsl
    10000                               -- @timeout (10 seconds)
);
```

You can also encapsulate it with default SMTP configurations:

```sql
CREATE OR ALTER FUNCTION dbo.InterfaceEmail
(
    @smtpHost NVARCHAR(4000) = NULL,
    @smtpPort INT = NULL,
    @smtpUser NVARCHAR(4000) = NULL,
    @smtpPass NVARCHAR(4000) = NULL,
    @from NVARCHAR(4000),
    @to NVARCHAR(4000),
    @subject NVARCHAR(4000),
    @body NVARCHAR(MAX),
    @enableSsl BIT = NULL,
    @timeout INT = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT success, message, timing, exceptionType
    FROM dbo.EmailSender(
        ISNULL(@smtpHost,  'smtp.office365.com'),
        ISNULL(@smtpPort,  587),
        ISNULL(@smtpUser,  'user@company.com.br'),
        ISNULL(@smtpPass,  'password'),
        @from,
        @to,
        @subject,
        @body,
        ISNULL(@enableSsl, 1),
        ISNULL(@timeout,   10000)
    )
);
```

Example with defaults:

```sql
SELECT * FROM master.dbo.InterfaceEmail(
    null,                               -- @smtpHost
    null,                               -- @smtpPort
    null,                               -- @smtpUser
    null,                               -- @smtpPass
    'System <system@company.com.br>',   -- @from
    'recipient@domain.com',             -- @to
    'Subject via CLR',                  -- @subject
    'Email body',                       -- @body
    1,                                  -- @enableSsl
    10000                               -- @timeout (10 seconds)
);
```

---

## **Security and Best Practices**

- Only trusted users or database roles should have access to this assembly and the function.
- This function requires `UNSAFE` CLR permissions due to SMTP access.
- Always validate external data and minimize exposure to sensitive information in credentials and content.

---

## **Project Motivation**

While SQL Server offers built-in mail features, they are limited, complex to configure, or not suitable for all environments.  
**EmailSenderLibrary** provides a robust, high-performance, and maintainable solution that works across all supported SQL Server versions—modern or legacy—offering a reliable and secure way to send emails from T-SQL code.

---

## **License**

[MIT License](LICENSE)

---

## **Contributing**

Contributions are welcome! Please open issues or pull requests for improvements, fixes, or new features.

---

## **Authors**

Developed and maintained by Rodrigo Kmiecik.

---
