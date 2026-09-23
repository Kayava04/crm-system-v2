using Shared.Files;

namespace Students.Application.Services;

// The student table as a person sees it in Excel: readable names in English and Ukrainian, readable values
internal static class StudentTable
{
    public static readonly Choice[] LearningGoals =
    [
        new("Work", "Work", "Робота"),
        new("Relocation", "Relocation", "Переїзд"),
        new("Study", "Study", "Навчання"),
        new("Personal", "Personal", "Особисті цілі"),
    ];

    public static readonly Choice[] Formats =
    [
        new("Online", "Online", "Онлайн"),
        new("Offline", "Offline", "Офлайн"),
    ];

    public static readonly Choice[] LessonTypes =
    [
        new("Individual", "Individual", "Індивідуальні"),
        new("Group", "Group", "Групові"),
    ];

    public static readonly Choice[] Levels =
        new[] { "A1", "A2", "B1", "B2", "C1", "C2" }.Select(l => new Choice(l, l, l)).ToArray();

    public static readonly Choice[] Languages =
    [
        new("English", "English", "Англійська"),
        new("French", "French", "Французька"),
        new("German", "German", "Німецька"),
        new("Polish", "Polish", "Польська"),
        new("Spanish", "Spanish", "Іспанська"),
        new("Italian", "Italian", "Італійська"),
    ];

    public static readonly Choice[] Statuses =
    [
        new("Active", "Active", "Активний"),
        new("Suspended", "Suspended", "Призупинений"),
        new("Graduated", "Graduated", "Випускник"),
        new("Withdrawn", "Withdrawn", "Вибув"),
    ];

    public static readonly TableSchema Schema = new("Students", "Студенти",
    [
        new ColumnDef("firstName", "First name", "Ім'я", ColumnType.Text, Required: true,
            ExampleEn: "Ivan", ExampleUk: "Іван", NoteEn: "Up to 50 characters.", NoteUk: "До 50 символів."),
        new ColumnDef("lastName", "Last name", "Прізвище", ColumnType.Text, Required: true,
            ExampleEn: "Petrenko", ExampleUk: "Петренко", NoteEn: "Up to 50 characters.", NoteUk: "До 50 символів."),
        new ColumnDef("middleName", "Middle name", "По батькові", ColumnType.Text,
            ExampleEn: "Andriyovych", ExampleUk: "Андрійович", NoteEn: "Optional.", NoteUk: "Необов'язково."),
        new ColumnDef("dateOfBirth", "Date of birth", "Дата народження", ColumnType.Date, Required: true,
            ExampleEn: "2001-03-25", ExampleUk: "2001-03-25", NoteEn: "Must be in the past.", NoteUk: "Має бути в минулому."),
        new ColumnDef("phoneNumber", "Phone", "Телефон", ColumnType.Text, Required: true,
            ExampleEn: "+380501112233", ExampleUk: "+380501112233", NoteEn: "7-20 digits, may start with +.", NoteUk: "7-20 цифр, може починатися з +."),
        new ColumnDef("email", "Email", "Електронна пошта", ColumnType.Text, Required: true,
            ExampleEn: "ivan.petrenko@example.com", ExampleUk: "ivan.petrenko@example.com", NoteEn: "Must be unique in the system.", NoteUk: "Має бути унікальною в системі."),
        new ColumnDef("city", "City", "Місто", ColumnType.Text, Required: true, ExampleEn: "Kyiv", ExampleUk: "Київ"),
        new ColumnDef("country", "Country", "Країна", ColumnType.Text, Required: true, ExampleEn: "Ukraine", ExampleUk: "Україна"),
        new ColumnDef("isChild", "Is a child", "Дитина", ColumnType.Boolean,
            ExampleEn: "No", ExampleUk: "Ні", NoteEn: "Empty means No.", NoteUk: "Порожньо означає «Ні»."),
        new ColumnDef("learningGoal", "Learning goal", "Мета навчання", ColumnType.Choice, Required: true, Choices: LearningGoals,
            ExampleEn: "Work", ExampleUk: "Робота"),
        new ColumnDef("format", "Lesson format", "Формат занять", ColumnType.Choice, Required: true, Choices: Formats,
            ExampleEn: "Online", ExampleUk: "Онлайн"),
        new ColumnDef("lessonType", "Lesson type", "Тип занять", ColumnType.Choice, Required: true, Choices: LessonTypes,
            ExampleEn: "Individual", ExampleUk: "Індивідуальні"),
        new ColumnDef("intensity", "Intensity (1-7)", "Інтенсивність (1-7)", ColumnType.Integer, Required: true,
            ExampleEn: "3", ExampleUk: "3", NoteEn: "A whole number from 1 to 7.", NoteUk: "Ціле число від 1 до 7."),
        new ColumnDef("currentLevel", "Current level", "Поточний рівень", ColumnType.Choice, Required: true, Choices: Levels,
            ExampleEn: "B1", ExampleUk: "B1"),
        new ColumnDef("hadPreviousCourses", "Had previous courses", "Були попередні курси", ColumnType.Boolean,
            ExampleEn: "No", ExampleUk: "Ні", NoteEn: "Empty means No.", NoteUk: "Порожньо означає «Ні»."),
        new ColumnDef("languages", "Languages", "Мови, що вивчає", ColumnType.MultiChoice, Required: true, Choices: Languages,
            ExampleEn: "English, German", ExampleUk: "Англійська, Німецька",
            NoteEn: "One or more, separated by commas.", NoteUk: "Одна або кілька, через кому."),
        new ColumnDef("comment", "Comment", "Коментар", ColumnType.Text,
            ExampleEn: "Prefers evening lessons", ExampleUk: "Віддає перевагу вечірнім заняттям", NoteEn: "Optional, up to 500 characters.", NoteUk: "Необов'язково, до 500 символів."),

        // present in exports only
        new ColumnDef("status", "Status", "Статус", ColumnType.Choice, ForImport: false, Choices: Statuses),
        new ColumnDef("hasAccount", "Has an account", "Має акаунт", ColumnType.Boolean, ForImport: false),
        new ColumnDef("createdAt", "Created", "Дата створення", ColumnType.DateTime, ForImport: false),
    ]);
}
