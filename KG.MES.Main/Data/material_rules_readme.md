markdown
# Конфигурация типов материалов

Файл `material_types_rules.json` управляет типами материалов и правилами их определения.

## Структура файла

```json
{
  "types": { ... },
  "rules": [ ... ]
}
```

## Типы материалов (types)

Словарь, где ключ - уникальный идентификатор типа, значение - описание типа.

**Поле**		**Обязательное**		**Описание**							**Пример**
description		✅					Отображаемое название					"Пиломатериалы"
icon			❌					Иконка Bootstrap Icons					"bi-tree-fill"
color			❌					Цвет Bootstrap							"success", "primary", "secondary"
defaultUnit		❌					Единица измерения по умолчанию			"м", "м²", "кг", "шт", "Литры"

### Пример

```json
"Lumber": {
  "description": "Пиломатериалы",
  "icon": "bi-tree-fill",
  "color": "success",
  "defaultUnit": "м"
}
```

## Правила определения (rules)

Массив правил, которые определяют, к какому типу отнести материал.

**Поле**			**Обязательное**		**Описание**
type						✅				Ключ типа из types
xmlSource					✅				Источник в XML (см. список ниже)
priority					❌				Приоритет (больше = важнее). По умолчанию 0
skip						❌				true - не добавлять в спецификацию
coefficient					❌				Множитель количества. По умолчанию 1
unit						❌				Переопределяет defaultUnit из типа
articleKeywords				❌				Список ключевых слов в артикуле
descriptionKeywords			❌				Список ключевых слов в описании

### Возможные значения xmlSource

**Значение**		**Описание**
XmlWoodProfile		Пиломатериалы (из профилей)
XmlPaint			ЛКМ (из блока paints)
XmlFitting			Фурнитура
XmlGlassProduct		Стеклопакеты
XmlAccessory		Аксессуары
XmlTechArticle		Технические артикулы

## Логика работы правил

Правила с ключевыми словами проверяются в порядке убывания priority

Если любое ключевое слово из **articleKeywords** или **descriptionKeywords** найдено в артикуле/описании материала → правило применяется

Если ключевые слова не заданы или не совпали → применяется правило без ключевых слов для данного xmlSource

Если правило не найдено → материал получает тип **Other**

### Примеры правил

#### 1. Простое правило (только по источнику)

```json
{
  "type": "Lumber",
  "xmlSource": "XmlWoodProfile"
}
```

#### 2. Правило с переопределением единицы измерения и коэффициентом

```json
{
  "type": "Packaging",
  "xmlSource": "XmlTechArticle",
  "unit": "кг",
  "coefficient": 0.001,
  "articleKeywords": ["стрейч", "пленк"]
}
```

#### 3. Правило с исключением (материал не попадает в спецификацию)

```json
{
  "type": "Paint",
  "xmlSource": "XmlPaint",
  "skip": true
}
```

#### 4. Правило с несколькими ключевыми словами (логика ИЛИ)

```json
{
  "type": "Seal",
  "xmlSource": "XmlTechArticle",
  "articleKeywords": ["deventer", "s7503", "us1"],
  "descriptionKeywords": ["уплотнитель", "упл"]
}
```

#### Пример полной конфигурации

```json
{
  "types": {
    "Lumber": { "description": "Пиломатериалы", "defaultUnit": "м" },
    "Paint": { "description": "ЛКМ", "defaultUnit": "Литры" },
    "Packaging": { "description": "Упаковка", "defaultUnit": "м" },
    "Other": { "description": "Прочее", "defaultUnit": "шт" }
  },
  "rules": [
    {
      "type": "Lumber",
      "xmlSource": "XmlWoodProfile"
    },
    {
      "type": "Paint",
      "xmlSource": "XmlPaint",
      "skip": true
    },
    {
      "type": "Packaging",
      "xmlSource": "XmlTechArticle",
      "priority": 10,
      "unit": "кг",
      "coefficient": 0.001,
      "articleKeywords": ["стрейч"],
      "descriptionKeywords": ["пленка"]
    }
  ]
}
```
