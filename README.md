# Приложение по обработке базы данных для хранения информации о составах выпускаемых компонентов

Разработана база знаний для хранения информации о составах выпускаемых компонентов, а также программа по её обработке.

![2](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/2.jpg)

## База данных

Взаимодействие с базой данных в коде реализовано с использованием [Linq](https://docs.microsoft.com/ru-ru/dotnet/csharp/linq/).

Бекап базы данных приведен в [соответствующем файле](https://github.com/lenskii/ASCON_TestApp/blob/master/db.bak).

Записи хранятся в БД на сервере MS SQL в таблицах `Components` и `ComponentsList`:

Имеющиеся поля таблицы `Components`:
* Уникальный идентификатор `Id` типа `bigint`
* Id родительского компонента `ParentId` типа `bigint`
* Id уникального `ComponentId` типа `bigint` - используется для связи со второй таблицей `ComponentsList`
* Имя компонента `Name` типа `varchar(50)` - для корневых компонентов
* Количество вхождений данного компонента `Amount` типа `bigint`

Имеющиеся поля таблицы `ComponentsList`:
* Уникальный идентификатор `Id` типа `bigint` - используется для связи с первой таблицей `Components`
* Имя компонента `Name` типа `varchar(50)` 

![1](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/1.jpg)

## Пользовательский интерфейс

Написан на C# WPF.

Среди имеющихся возможностей: изменение, удаление записей с помощью контекстного меню:

| Общее контекстное меню  | Контекстное меню компонента |
| ------------- | ------------- |
| ![3](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/3.jpg)  | ![4](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/4.jpg)  |

* Добавление компонентов вложенных уровней с указанием количества вхождения

Есть возможность добавить уже существующий компонент из списка, либо ввести имя нового:

| ![5](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/5.jpg) | ![6](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/6.jpg) |
| ------------- | ------------- |

Обработаны исключения на нулевые значения и тип вводимых параметров:

|  ![7](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/7.jpg)  | ![8](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/8.jpg)  |
| ------------- | ------------- |
|  ![9](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/9.jpg)  |  |

* Добавление новых компонентов верхнего уровня с обработкой исключений:

 ![13](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/13.jpg) 

* Переименование компонентов

 ![10](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/10.jpg) 
     
* Удаление компонентов

| ![11](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/11.jpg) |  ![14](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/14.jpg) |
| ------------- | ------------- |
 
* Выгрузка в MS Word отчета о сводном составе указанного компонента

 ![12](https://github.com/lenskii/ASCON_TestApp/blob/master/docs/12.jpg) 

Последний доступный релиз можно загрузить по соответствующей [ссылке](https://github.com/lenskii/ASCON_TestApp/releases/tag/1.0).

---

Приветствуются любые комментарии и пожелания:

* lenskii97@gmail.com
* [Telegram](https://t.me/lenskii97)
