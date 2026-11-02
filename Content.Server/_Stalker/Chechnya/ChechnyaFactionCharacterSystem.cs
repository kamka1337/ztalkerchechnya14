using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Stalker.Chechnya;

public sealed class ChechnyaFactionCharacterSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly HashSet<string> _usedFullNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _usedSurnames = new(StringComparer.OrdinalIgnoreCase);

    private static readonly HumanTonedSkinColoration SkinColoration = new();

    #region CRI Data (40 First Names, 150 Surnames)

    public static readonly string[] CriFirstNames =
    {
        "Шамиль", "Руслан", "Магомед", "Аслан", "Муса",
        "Иса", "Зелимхан", "Тимур", "Хамзат", "Рамзан",
        "Анзор", "Беслан", "Тамерлан", "Ахмед", "Ваха",
        "Салман", "Джохар", "Апти", "Султан", "Муслим",
        "Умар", "Абубакар", "Хасан", "Хусейн", "Али",
        "Мансур", "Казбек", "Ибрагим", "Адам", "Заур",
        "Турпал", "Рустам", "Булат", "Джамбулат", "Байсангур",
        "Мовсар", "Лечи", "Дауд", "Идрис", "Шахбулат"
    };

    public static readonly string[] CriSurnames =
    {
        "Басаев", "Радуев", "Дудаев", "Масхадов", "Гелаев",
        "Умаров", "Бараев", "Цакаев", "Кадыров", "Ямадаев",
        "Делимханов", "Алханов", "Дадаев", "Гакаев", "Исраилов",
        "Муртазалиев", "Чалаев", "Межидов", "Хучиев", "Магомадов",
        "Идрисов", "Ахмадов", "Сулейманов", "Хамзатов", "Джабраилов",
        "Мусаев", "Исаев", "Уциев", "Арсанов", "Темирбулатов",
        "Гайрбеков", "Эдильбиев", "Шамурзаев", "Батаев", "Бетербиев",
        "Газиев", "Даудов", "Завгаев", "Куриев", "Лорсанов",
        "Мациев", "Наурбиев", "Оздоев", "Садулаев", "Тасуев",
        "Устарханов", "Хаджиев", "Чимаев", "Шамсудинов", "Эльмурзаев",
        "Юсупов", "Яндарбиев", "Абубакаров", "Байсаров", "Вагапов",
        "Гойгов", "Дандаев", "Евлоев", "Жамалдаев", "Зангиев",
        "Ибишев", "Костоев", "Лабазанов", "Мальсагов", "Нухаев",
        "Орцуев", "Паскаев", "Рашидов", "Саидов", "Торшхоев",
        "Умаханов", "Ферзаули", "Хабибулаев", "Цороев", "Чабаев",
        "Шайхиев", "Эдилов", "Юнусов", "Якубов", "Алиев",
        "Баширов", "Вараев", "Гиреев", "Джамалдинов", "Елхоев",
        "Зубайраев", "Ибрагимов", "Кудухов", "Ломалиев", "Мутаев",
        "Насуханов", "Осмаев", "Плиев", "Раисов", "Саипов",
        "Таймасханов", "Умалатов", "Халидов", "Цугаев", "Чермоев",
        "Шаипов", "Эскерханов", "Юшаев", "Яричев", "Абдурахманов",
        "Базоркин", "Вахаев", "Гучигов", "Дугучиев", "Закаев",
        "Илаев", "Казиев", "Лулуев", "Мамакаев", "Назиров",
        "Омаров", "Патиев", "Рамазанов", "Салгириев", "Татриев",
        "Угурчиев", "Хасбулатов", "Цуроев", "Чичаев", "Шовхалов",
        "Эльдаров", "Яхъяев", "Акаев", "Бисултанов", "Ведзижев",
        "Галаев", "Дзауров", "Зармаев", "Гатагажев", "Караев",
        "Лаудаев", "Махмудов", "Нагаев", "Оздиев", "Пириев",
        "Русланбеков", "Салтаханов", "Тепсуев", "Ужахов", "Хататаев",
        "Цуев", "Чахкиев", "Шишханов", "Элимханов", "Яндиев"
    };

    private static readonly string[] CriHairStyles =
    {
        HairStyles.DefaultHairStyle.Id,
        "HumanHairShaved"
    };

    private static readonly string[] CriBeardStyles =
    {
        "HumanFacialHairFullbeard",
        "HumanFacialHairCroppedfullbeard",
        "HumanFacialHairChinlessbeard",
        "HumanFacialHairGt",
        "HumanFacialHairChin",
        "HumanFacialHairAbe",
        "HumanFacialHair3oclock",
        "HumanFacialHairFiveoclock",
        "HumanFacialHair5oclockmoustache",
        "HumanFacialHair7oclock",
        "HumanFacialHair7oclockmoustache",
        HairStyles.DefaultFacialHairStyle.Id
    };

    private static readonly Color[] CriHairColors =
    {
        Color.FromHex("#151515"),
        Color.FromHex("#1C1715"),
        Color.FromHex("#211F1D"),
        Color.FromHex("#25201E"),
        Color.FromHex("#3B2219"),
        Color.FromHex("#4A2E1B"),
        Color.FromHex("#54331C"),
        Color.FromHex("#5C3A21"),
        Color.FromHex("#432A1D")
    };

    private static readonly Color[] CriEyeColors =
    {
        Color.FromHex("#3F2613"),
        Color.FromHex("#26150B"),
        Color.FromHex("#4D3018"),
        Color.FromHex("#785028"),
        Color.FromHex("#614C32"),
        Color.FromHex("#52422D"),
        Color.FromHex("#3D4F38")
    };

    #endregion

    #region RF Data (30 Russian Names, 120 Russian + 30 Ukrainian Surnames)

    public static readonly string[] RfFirstNames =
    {
        "Александр", "Алексей", "Андрей", "Антон", "Артем",
        "Виктор", "Владимир", "Владислав", "Вячеслав", "Денис",
        "Дмитрий", "Евгений", "Егор", "Иван", "Игорь",
        "Илья", "Кирилл", "Константин", "Максим", "Михаил",
        "Николай", "Олег", "Павел", "Роман", "Сергей",
        "Станислав", "Тимофей", "Федор", "Юрий", "Ярослав"
    };

    public static readonly string[] RfRussianSurnames =
    {
        "Иванов", "Смирнов", "Кузнецов", "Попов", "Васильев",
        "Петров", "Соколов", "Михайлов", "Новиков", "Федоров",
        "Морозов", "Волков", "Алексеев", "Лебедев", "Семенов",
        "Егоров", "Павлов", "Козлов", "Степанов", "Николаев",
        "Орлов", "Андреев", "Макаров", "Никитин", "Захаров",
        "Зайцев", "Соловьев", "Борисов", "Яковлев", "Григорьев",
        "Романов", "Воробьев", "Сергеев", "Кузьмин", "Фролов",
        "Александров", "Дмитриев", "Королев", "Гусев", "Киселев",
        "Ильин", "Максимов", "Поляков", "Сорокин", "Виноградов",
        "Ковалев", "Белов", "Медведев", "Антонов", "Тарасов",
        "Жуков", "Баранов", "Филиппов", "Комаров", "Давыдов",
        "Беляев", "Герасимов", "Богданов", "Осипов", "Сидоров",
        "Матвеев", "Титов", "Марков", "Миронов", "Крылов",
        "Куликов", "Карпов", "Власов", "Мельников", "Денисов",
        "Гаврилов", "Тихонов", "Казаков", "Афанасьев", "Данилов",
        "Савельев", "Тимофеев", "Фомин", "Чернов", "Абрамов",
        "Мартынов", "Ефимов", "Федотов", "Шарапов", "Воронов",
        "Лазарев", "Горбунов", "Филатов", "Лукин", "Панфилов",
        "Рябов", "Широков", "Зимин", "Кудрявцев", "Рогов",
        "Щербаков", "Самойлов", "Капустин", "Овчинников", "Гордеев",
        "Дроздов", "Трофимов", "Архипов", "Савин", "Бабушкин",
        "Родионов", "Калинин", "Логинов", "Калашников", "Суворов",
        "Гришин", "Голубев", "Прохоров", "Мухин", "Нестеров",
        "Гурьев", "Дорофеев", "Платонов", "Румянцев", "Панкратов"
    };

    public static readonly string[] RfUkrainianSurnames =
    {
        "Шевченко", "Бондаренко", "Коваленко", "Бойко", "Кравченко",
        "Ткаченко", "Ковальчук", "Мельник", "Олейник", "Мороз",
        "Гончаренко", "Сидоренко", "Пономаренко", "Гриценко", "Марченко",
        "Лысенко", "Демченко", "Романенко", "Боротько", "Котельник",
        "Назаренко", "Савченко", "Гаврилюк", "Василенко", "Харченко",
        "Дорошенко", "Пасечник", "Белоус", "Кириленко", "Руденко"
    };

    public static readonly string[] RfAllSurnames = RfRussianSurnames.Concat(RfUkrainianSurnames).ToArray();

    private static readonly string[] RfHairStyles =
    {
        HairStyles.DefaultHairStyle.Id,
        "HumanHairShaved"
    };

    private static readonly Color[] RfHairColors =
    {
        // Black
        Color.FromHex("#151515"),
        Color.FromHex("#1E1C1B"),
        // Chestnut / Brown
        Color.FromHex("#3D2517"),
        Color.FromHex("#4F301F"),
        Color.FromHex("#613F29"),
        Color.FromHex("#704A33"),
        Color.FromHex("#5A3C28"),
        // Blond
        Color.FromHex("#C8A86C"),
        Color.FromHex("#D6B57E"),
        Color.FromHex("#DFC494"),
        Color.FromHex("#BFA06B"),
        Color.FromHex("#E6CE9F")
    };

    private static readonly Color[] RfEyeColors =
    {
        Color.FromHex("#4F729E"),
        Color.FromHex("#6B8DB5"),
        Color.FromHex("#637280"),
        Color.FromHex("#4E684D"),
        Color.FromHex("#6E593A"),
        Color.FromHex("#4A2F1B"),
        Color.FromHex("#2D1B10")
    };

    #endregion

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _usedFullNames.Clear();
        _usedSurnames.Clear();
    }

    public bool IsChechnyaJob(string? jobId)
    {
        return jobId != null && (jobId.StartsWith("ChechnyaRF", StringComparison.Ordinal) || jobId.StartsWith("ChechnyaCRI", StringComparison.Ordinal));
    }

    public bool IsChechnyaCRI(string? jobId)
    {
        return jobId != null && jobId.StartsWith("ChechnyaCRI", StringComparison.Ordinal);
    }

    public bool IsChechnyaRF(string? jobId)
    {
        return jobId != null && jobId.StartsWith("ChechnyaRF", StringComparison.Ordinal);
    }

    public HumanoidCharacterProfile GenerateFactionProfile(string jobId, HumanoidCharacterProfile? baseProfile = null)
    {
        var isCri = IsChechnyaCRI(jobId);

        var fullName = isCri
            ? PickUniqueName(CriFirstNames, CriSurnames)
            : PickUniqueName(RfFirstNames, RfAllSurnames);

        var skinColor = isCri
            ? SkinColoration.FromUnary(_random.NextFloat(42f, 56f))
            : SkinColoration.FromUnary(_random.NextFloat(6f, 18f));

        var hairStyle = isCri
            ? _random.Pick(CriHairStyles)
            : _random.Pick(RfHairStyles);

        var hairColor = isCri
            ? _random.Pick(CriHairColors)
            : _random.Pick(RfHairColors);

        var facialHairStyle = isCri
            ? _random.Pick(CriBeardStyles)
            : HairStyles.DefaultFacialHairStyle.Id;

        var facialHairColor = isCri
            ? hairColor
            : Color.Black;

        var eyeColor = isCri
            ? _random.Pick(CriEyeColors)
            : _random.Pick(RfEyeColors);

        var age = _random.Next(18, 45);

        var appearance = new HumanoidCharacterAppearance(
            hairStyle,
            hairColor,
            facialHairStyle,
            facialHairColor,
            eyeColor,
            skinColor,
            new List<Marking>());

        var profile = baseProfile != null
            ? new HumanoidCharacterProfile(baseProfile)
            : HumanoidCharacterProfile.DefaultWithSpecies();

        profile.Name = fullName;
        profile.Age = age;
        profile.Appearance = appearance;
        profile.Changeable = false;

        return profile.WithSex(Sex.Male).WithGender(Gender.Male);
    }

    private string PickUniqueName(IReadOnlyList<string> firstNames, IReadOnlyList<string> surnames)
    {
        // 1. Try to pick an unused surname first to maximize surname uniqueness
        var unusedSurnames = new List<string>();
        foreach (var s in surnames)
        {
            if (!_usedSurnames.Contains(s))
                unusedSurnames.Add(s);
        }

        if (unusedSurnames.Count > 0)
        {
            var shuffledUnused = new List<string>(unusedSurnames);
            _random.Shuffle(shuffledUnused);

            var shuffledFirst = new List<string>(firstNames);
            _random.Shuffle(shuffledFirst);

            foreach (var s in shuffledUnused)
            {
                foreach (var f in shuffledFirst)
                {
                    var full = $"{f} {s}";
                    if (!_usedFullNames.Contains(full))
                    {
                        _usedFullNames.Add(full);
                        _usedSurnames.Add(s);
                        return full;
                    }
                }
            }
        }

        // 2. All surnames used at least once; pick any combination that has not been used yet in the round
        var allSurnamesShuffled = new List<string>(surnames);
        _random.Shuffle(allSurnamesShuffled);

        var allFirstShuffled = new List<string>(firstNames);
        _random.Shuffle(allFirstShuffled);

        foreach (var s in allSurnamesShuffled)
        {
            foreach (var f in allFirstShuffled)
            {
                var full = $"{f} {s}";
                if (!_usedFullNames.Contains(full))
                {
                    _usedFullNames.Add(full);
                    _usedSurnames.Add(s);
                    return full;
                }
            }
        }

        // Ultra fallback
        var fallbackFirst = firstNames[_random.Next(firstNames.Count)];
        var fallbackSurname = surnames[_random.Next(surnames.Count)];
        var fallbackFull = $"{fallbackFirst} {fallbackSurname}";
        _usedFullNames.Add(fallbackFull);
        return fallbackFull;
    }
}
