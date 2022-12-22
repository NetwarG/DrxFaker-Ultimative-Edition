# Решение "Генератор сущностей"
## Описание
Решение предназначено для генерации справочников и документов по настраиваемым пользовательским шаблонам.

Состав объектов разработки:
* Модуль с обложкой "Генератор сущностей";
* Справочник "Соответствие заполняемых параметров сущности";
* Справочник "Типы справочников".

## Ссылка на подробное описание решения:
[Решение «Генератор сущностей»](https://club.directum.ru/post/361314)

## Варианты расширения функциональности на проектах:
1.	Добавление возможности выбора языка для генерации данных;
2.	Добавление возможности автоматического заполнения обязательных полей, которые становятся обязательными только при показе формы;
3.	Добавление новых вариантов заполнения для свойств сущностей;
4.	Изменение существующих вариантов заполнения для свойств сущностей.

## Порядок установки
Для работы требуется установленный Directum Development studio версии 3.6 и выше.

### Установка для ознакомления
1. Склонировать репозиторий DrxFaker в папку (например C:\WorkFolder).
2. Указать в _ConfigSettings.xml DDS:
```xml
<block name="REPOSITORIES">
  <repository folderName="Base" solutionType="Base" url="<адрес локального репозитория>" />
  <repository folderName="Work" solutionType="Work" url="<адрес локального репозитория>" />
  <repository folderName="<Папка из п.1>" solutionType="Work" url="https://github.com/STARKOV-Group/DrxFaker" />
</block>
```

### Установка для использования на проекте
Возможные варианты:

#### A. Fork репозитория
1. Сделать fork репозитория DrxFaker для своей учетной записи.
2. Склонировать созданный в п. 1 репозиторий в папку.
3. Указать в _ConfigSettings.xml DDS:
```xml
<block name="REPOSITORIES">
  <repository folderName="Base" solutionType="Base" url="<адрес локального репозитория>" />
  <repository folderName="Work" solutionType="Work" url="<адрес локального репозитория>" />
  <repository folderName="<Папка из п.2>" solutionType="Work" url="<Адрес репозитория gitHub учетной записи пользователя из п. 1>" />
</block>
```

#### B. Подключение на базовый слой.
Вариант не рекомендуется:
* так как при выходе новой версии шаблона разработки не гарантируется обратная совместимость;
* потеряется возможность изменения или доработки функционала под собственые требования;


1. Склонировать репозиторий DrxFaker в папку.
2. Указать в _ConfigSettings.xml DDS:
```xml
<block name="REPOSITORIES">
  <repository folderName="Base" solutionType="Base" url="" /> 
  <repository folderName="<Папка из п.1>" solutionType="Base" url="<Адрес репозитория gitHub>" />
  <repository folderName="<Папка для рабочего слоя>" solutionType="Work" url="https://github.com/STARKOV-Group/DrxFaker" />
</block>
```

#### C. Копирование репозитория в систему контроля версий.
Рекомендуемый вариант для проектов внедрения.

1. В системе контроля версий с поддержкой git создать новый репозиторий.
2. Склонировать репозиторий Settings в папку с ключом ```--mirror```.
3. Перейти в папку из п. 2.
4. Импортировать клонированный репозиторий в систему контроля версий командой:
```
git push –mirror <Адрес репозитория из п. 1>
```