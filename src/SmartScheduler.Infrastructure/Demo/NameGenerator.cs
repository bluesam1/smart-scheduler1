namespace SmartScheduler.Infrastructure.Demo;

/// <summary>
/// Generates realistic contractor names with variety.
/// </summary>
public static class NameGenerator
{
    private static readonly string[] FirstNames = new[]
    {
        "James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph", "Thomas", "Charles",
        "Christopher", "Daniel", "Matthew", "Anthony", "Donald", "Mark", "Paul", "Steven", "Andrew", "Kenneth",
        "Joshua", "Kevin", "Brian", "George", "Edward", "Ronald", "Timothy", "Jason", "Jeffrey", "Ryan",
        "Jacob", "Gary", "Nicholas", "Eric", "Jonathan", "Stephen", "Larry", "Justin", "Scott", "Brandon",
        "Mary", "Patricia", "Jennifer", "Linda", "Barbara", "Elizabeth", "Susan", "Jessica", "Sarah", "Karen",
        "Nancy", "Lisa", "Betty", "Margaret", "Sandra", "Ashley", "Kimberly", "Emily", "Donna", "Michelle",
        "Dorothy", "Carol", "Amanda", "Melissa", "Deborah", "Stephanie", "Rebecca", "Sharon", "Laura", "Cynthia",
        "Kathleen", "Amy", "Angela", "Shirley", "Anna", "Brenda", "Pamela", "Emma", "Nicole", "Helen",
        "Samantha", "Katherine", "Christine", "Debra", "Rachel", "Catherine", "Carolyn", "Janet", "Ruth", "Maria",
        "Heather", "Diane", "Virginia", "Julie", "Joyce", "Victoria", "Olivia", "Kelly", "Christina", "Lauren"
    };

    private static readonly string[] LastNames = new[]
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
        "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
        "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson",
        "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
        "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell", "Carter", "Roberts",
        "Gomez", "Phillips", "Evans", "Turner", "Diaz", "Parker", "Cruz", "Edwards", "Collins", "Reyes",
        "Stewart", "Morris", "Morales", "Murphy", "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper",
        "Peterson", "Bailey", "Reed", "Kelly", "Howard", "Ramos", "Kim", "Cox", "Ward", "Richardson",
        "Watson", "Brooks", "Chavez", "Wood", "James", "Bennett", "Gray", "Mendoza", "Ruiz", "Hughes",
        "Price", "Alvarez", "Castillo", "Sanders", "Patel", "Myers", "Long", "Ross", "Foster", "Jimenez"
    };

    /// <summary>
    /// Generates a random contractor name with timestamp suffix to ensure uniqueness.
    /// </summary>
    public static string GenerateUniqueName(Random random, DateTime timestamp)
    {
        var firstName = FirstNames[random.Next(FirstNames.Length)];
        var lastName = LastNames[random.Next(LastNames.Length)];
        var timestampSuffix = timestamp.ToString("HHmmss");
        
        return $"{firstName} {lastName} ({timestampSuffix})";
    }

    /// <summary>
    /// Generates a random contractor name without suffix.
    /// </summary>
    public static string GenerateName(Random random)
    {
        var firstName = FirstNames[random.Next(FirstNames.Length)];
        var lastName = LastNames[random.Next(LastNames.Length)];
        
        return $"{firstName} {lastName}";
    }
}

