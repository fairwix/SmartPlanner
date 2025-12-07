using FluentMigrator;

[Migration(0003)]
public class AddConstraintsAndComputedColumns : Migration
{
    public override void Up()
    {
        // Проверочные ограничения
        Execute.Sql(@"
            ALTER TABLE ""Goals""
            ADD CONSTRAINT ""CK_Goals_TargetValue_Positive""
            CHECK (""TargetValue"" > 0);

            ALTER TABLE ""Goals""
            ADD CONSTRAINT ""CK_Goals_CurrentValue_Range""
            CHECK (""CurrentValue"" >= 0 AND ""CurrentValue"" <= ""TargetValue"");

            ALTER TABLE ""Challenges""
            ADD CONSTRAINT ""CK_Challenges_Dates""
            CHECK (""EndDate"" > ""StartDate"");

            ALTER TABLE ""Users""
            ADD CONSTRAINT ""CK_Users_Balance_NonNegative""
            CHECK (""Balance"" >= 0);
        ");

        // Генерируемые столбцы (опционально)
        Execute.Sql(@"
            ALTER TABLE ""Goals""
            ADD COLUMN ""ProgressPercentage"" DECIMAL(5,2)
            GENERATED ALWAYS AS (
                CASE
                    WHEN ""TargetValue"" > 0
                    THEN (""CurrentValue"" * 100.0) / ""TargetValue""
                    ELSE 0
                END
            ) STORED;

            ALTER TABLE ""Challenges""
            ADD COLUMN ""GroupProgressPercentage"" DECIMAL(5,2)
            GENERATED ALWAYS AS (
                CASE
                    WHEN ""TargetValue"" > 0
                    THEN (""CurrentValue"" * 100.0) / ""TargetValue""
                    ELSE 0
                END
            ) STORED;
        ");
    }

    public override void Down()
    {
        Execute.Sql(@"
            ALTER TABLE ""Goals"" DROP CONSTRAINT ""CK_Goals_TargetValue_Positive"";
            ALTER TABLE ""Goals"" DROP CONSTRAINT ""CK_Goals_CurrentValue_Range"";
            ALTER TABLE ""Challenges"" DROP CONSTRAINT ""CK_Challenges_Dates"";
            ALTER TABLE ""Users"" DROP CONSTRAINT ""CK_Users_Balance_NonNegative"";

            ALTER TABLE ""Goals"" DROP COLUMN ""ProgressPercentage"";
            ALTER TABLE ""Challenges"" DROP COLUMN ""GroupProgressPercentage"";
        ");
    }
}
