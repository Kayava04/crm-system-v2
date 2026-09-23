using Shared.Files;

namespace Teachers.Application.Services;

// The teacher table as a person sees it in Excel: readable names in English and Ukrainian, readable values
internal static class TeacherTable
{
    public static readonly Choice[] Statuses =
    [
        new("Probation", "Probation", "Випробувальний термін"),
        new("Employed", "Employed", "Працює"),
        new("OnLeave", "On leave", "У відпустці"),
        new("Resigned", "Resigned", "Звільнився"),
        new("Dismissed", "Dismissed", "Звільнений"),
    ];

    public static readonly TableSchema Schema = new("Teachers", "Викладачі",
    [
        new ColumnDef("firstName", "First name", "Ім'я", ColumnType.Text, Required: true,
            ExampleEn: "Olena", ExampleUk: "Олена", NoteEn: "Up to 50 characters.", NoteUk: "До 50 символів."),
        new ColumnDef("lastName", "Last name", "Прізвище", ColumnType.Text, Required: true,
            ExampleEn: "Kovalenko", ExampleUk: "Коваленко", NoteEn: "Up to 50 characters.", NoteUk: "До 50 символів."),
        new ColumnDef("middleName", "Middle name", "По батькові", ColumnType.Text,
            ExampleEn: "Petrivna", ExampleUk: "Петрівна", NoteEn: "Optional.", NoteUk: "Необов'язково."),
        new ColumnDef("dateOfBirth", "Date of birth", "Дата народження", ColumnType.Date, Required: true,
            ExampleEn: "1988-07-14", ExampleUk: "1988-07-14", NoteEn: "Must be in the past.", NoteUk: "Має бути в минулому."),
        new ColumnDef("phoneNumber", "Phone", "Телефон", ColumnType.Text, Required: true,
            ExampleEn: "+380671234567", ExampleUk: "+380671234567", NoteEn: "7-20 digits, may start with +.", NoteUk: "7-20 цифр, може починатися з +."),
        new ColumnDef("email", "Email", "Електронна пошта", ColumnType.Text, Required: true,
            ExampleEn: "olena.kovalenko@example.com", ExampleUk: "olena.kovalenko@example.com", NoteEn: "Must be unique in the system.", NoteUk: "Має бути унікальною в системі."),
        new ColumnDef("city", "City", "Місто", ColumnType.Text, Required: true, ExampleEn: "Kyiv", ExampleUk: "Київ"),
        new ColumnDef("country", "Country", "Країна", ColumnType.Text, Required: true, ExampleEn: "Ukraine", ExampleUk: "Україна"),
        new ColumnDef("baseSalary", "Base salary", "Базова ставка", ColumnType.Decimal, Required: true,
            ExampleEn: "20000", ExampleUk: "20000", NoteEn: "Monthly, greater than 0. Becomes the first salary rate.", NoteUk: "На місяць, більше 0. Стає першою ставкою."),
        new ColumnDef("lessonsRate", "Rate per lesson", "Ставка за заняття", ColumnType.Decimal, Required: true,
            ExampleEn: "350", ExampleUk: "350", NoteEn: "Paid for each completed lesson, greater than 0.", NoteUk: "Оплата за кожне проведене заняття, більше 0."),
        new ColumnDef("comment", "Comment", "Коментар", ColumnType.Text,
            ExampleEn: "Speaks German too", ExampleUk: "Знає також німецьку", NoteEn: "Optional, up to 500 characters.", NoteUk: "Необов'язково, до 500 символів."),

        // present in exports only
        new ColumnDef("status", "Status", "Статус", ColumnType.Choice, ForImport: false, Choices: Statuses),
        new ColumnDef("hasAccount", "Has an account", "Має акаунт", ColumnType.Boolean, ForImport: false),
        new ColumnDef("createdAt", "Created", "Дата створення", ColumnType.DateTime, ForImport: false),
    ]);
}
