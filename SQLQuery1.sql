-- 1. Get the IDs for the user and the Admin role
DECLARE @UserId nvarchar(450) = (SELECT Id FROM AspNetUsers WHERE Email = 'admin@tacotales.com')
DECLARE @RoleId nvarchar(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin')

-- 2. If the 'Admin' role doesn't exist yet, create it
IF (@RoleId IS NULL)
BEGIN
    SET @RoleId = CAST(NEWID() AS nvarchar(450))
    INSERT INTO AspNetRoles (Id, Name, NormalizedName) 
    VALUES (@RoleId, 'Admin', 'ADMIN')
END

-- 3. Link the user to the role if they aren't already linked
IF (@UserId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId))
BEGIN
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)
END