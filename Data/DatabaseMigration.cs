using System;
using Npgsql;

namespace GymManagementSystem.Data
{
    public static class DatabaseMigration
    {
        private const string ConnectionString = "Host=ep-spring-hill-a4gxrjyd-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_biCL6PqYxl3Q;SSL Mode=Require";

        public static void ApplyMigrations()
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    // Check if column exists
                    var checkColumnQuery = @"
                        SELECT COUNT(*) 
                        FROM information_schema.columns 
                        WHERE table_name = 'Members' 
                        AND column_name = 'AssignedPackageId'";

                    using (var checkCmd = new NpgsqlCommand(checkColumnQuery, connection))
                    {
                        var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                        if (!exists)
                        {
                            Console.WriteLine("Adding AssignedPackageId column to Members table...");

                            // Add the column
                            var addColumnQuery = @"
                                ALTER TABLE ""Members"" 
                                ADD COLUMN ""AssignedPackageId"" INTEGER NULL";

                            using (var addCmd = new NpgsqlCommand(addColumnQuery, connection))
                            {
                                addCmd.ExecuteNonQuery();
                            }

                            // Add foreign key constraint
                            var addFKQuery = @"
                                ALTER TABLE ""Members""
                                ADD CONSTRAINT ""FK_Members_MembershipPackages_AssignedPackageId""
                                FOREIGN KEY (""AssignedPackageId"") 
                                REFERENCES ""MembershipPackages""(""PackageId"")
                                ON DELETE SET NULL";

                            try
                            {
                                using (var fkCmd = new NpgsqlCommand(addFKQuery, connection))
                                {
                                    fkCmd.ExecuteNonQuery();
                                }
                            }
                            catch (Exception ex)
                            {
                                // Foreign key might already exist, ignore error
                                Console.WriteLine($"FK constraint info: {ex.Message}");
                            }

                            // Create index
                            var addIndexQuery = @"
                                CREATE INDEX IF NOT EXISTS ""IX_Members_AssignedPackageId"" 
                                ON ""Members""(""AssignedPackageId"")";

                            using (var idxCmd = new NpgsqlCommand(addIndexQuery, connection))
                            {
                                idxCmd.ExecuteNonQuery();
                            }

                            Console.WriteLine("Migration completed successfully!");
                        }
                        else
                        {
                            Console.WriteLine("AssignedPackageId column already exists.");
                        }
                    }
                    
                    // Migration 2: Add CustomPackageAmount column
                    var checkCustomAmountQuery = @"
                        SELECT COUNT(*) 
                        FROM information_schema.columns 
                        WHERE table_name = 'Members' 
                        AND column_name = 'CustomPackageAmount'";

                    using (var checkCmd = new NpgsqlCommand(checkCustomAmountQuery, connection))
                    {
                        var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                        if (!exists)
                        {
                            Console.WriteLine("Adding CustomPackageAmount column to Members table...");

                            var addColumnQuery = @"
                                ALTER TABLE ""Members"" 
                                ADD COLUMN ""CustomPackageAmount"" NUMERIC(18,2) NULL";

                            using (var addCmd = new NpgsqlCommand(addColumnQuery, connection))
                            {
                                addCmd.ExecuteNonQuery();
                            }

                            Console.WriteLine("CustomPackageAmount column added successfully!");
                        }
                        else
                        {
                            Console.WriteLine("CustomPackageAmount column already exists.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration error: {ex.Message}");
                throw;
            }
        }
    }
}
