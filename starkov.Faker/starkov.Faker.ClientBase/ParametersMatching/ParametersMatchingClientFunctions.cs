﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using starkov.Faker.ParametersMatching;
using System.Text.RegularExpressions;
using CommonLibrary;

namespace starkov.Faker.Client
{
  partial class ParametersMatchingFunctions
  {
    
    /// <summary>
    /// Показ диалога для выбора данных.
    /// </summary>
    /// <param name="rowId">Номер строки.</param>
    /// <param name="isFillValue">Признак заполнения значений.</param>
    public void ShowDialogForSelectParameters(long? rowId, bool isFillValue)
    {
      ShowDialogForSelectParameters(rowId, isFillValue, false);
    }

    /// <summary>
    /// Показ диалога для выбора данных.
    /// </summary>
    /// <param name="rowId">Номер строки.</param>
    /// <param name="isFillValue">Признак заполнения значений.</param>
    /// <param name="isChange">Признак изменения значения в коллекции.</param>
    public void ShowDialogForSelectParameters(long? rowId, bool isFillValue, bool isChange)
    {
      var dialog = Dialogs.CreateInputDialog(starkov.Faker.ParametersMatchings.Resources.DialogDataInput);
      
      #region Данные для диалога
      var parameterRow = _obj.Parameters.FirstOrDefault(p => p.Id == rowId.GetValueOrDefault());
      var selectedPropertyNames = _obj.Parameters.Select(p => p.PropertyName);
      var propInfo = Functions.Module.Remote.GetPropertiesType(_obj.DatabookType?.DatabookTypeGuid ?? _obj.DocumentType?.DocumentTypeGuid)
        .Where(i => rowId.HasValue ?
               parameterRow.PropertyName == i.Name :
               isChange ? selectedPropertyNames.Contains(i.Name) : !selectedPropertyNames.Contains(i.Name));
      
      if (!propInfo.Any())
      {
        if (parameterRow == null)
          Dialogs.ShowMessage(starkov.Faker.ParametersMatchings.Resources.DialogInfoNoAvailableParams, MessageType.Information);
        else
          Dialogs.ShowMessage(starkov.Faker.ParametersMatchings.Resources.DialogErrorNoPropertyFormat(parameterRow.LocalizedPropertyName), MessageType.Error);
        return;
      }
      #endregion
      
      #region Поля диалога
      var localizedValuesField = dialog.AddSelect(starkov.Faker.ParametersMatchings.Resources.DialogFieldLocalizedValue, false)
        .From(propInfo.Select(i => i.LocalizedName).OrderBy(i => i).ToArray());
      var propertyNameField = dialog.AddSelect(starkov.Faker.ParametersMatchings.Resources.DialogFieldPropertyName, true)
        .From(propInfo.Select(i => i.Name).OrderBy(i => i).ToArray());
      var isLocalizedValues = dialog.AddBoolean(starkov.Faker.ParametersMatchings.Resources.DialogFieldUseLocalizedValue, false);
      var parameterField = dialog.AddSelect(starkov.Faker.ParametersMatchings.Resources.DialogFieldFillOption, true);
      var personalValuesField = new List<object>();
      
      var isUnique = propInfo.Select(i => i.LocalizedName).Count() == propInfo.Select(i => i.LocalizedName).Distinct().Count();
      isLocalizedValues.IsVisible = isUnique && parameterRow == null;
      parameterField.IsEnabled = parameterRow != null;
      #endregion
      
      #region Обработчики свойств
      dialog.SetOnRefresh((arg) =>
                          {
                            if (!isUnique)
                              arg.AddInformation(starkov.Faker.ParametersMatchings.Resources.DialogInfoLocalizedPropertyNotUnique);
                            
                            if (string.IsNullOrEmpty(propertyNameField.Value) || personalValuesField.Count != 2)
                              return;
                            
                            var selectedPropInfo = propInfo.FirstOrDefault(i => i.Name == propertyNameField.Value);
                            var customType = Functions.ParametersMatching.GetMatchingTypeToCustomType(selectedPropInfo.Type);
                            
                            if (customType == Constants.Module.CustomType.Date &&
                                Functions.Module.CastToDateDialogValue(personalValuesField[0])?.Value.GetValueOrDefault() >
                                Functions.Module.CastToDateDialogValue(personalValuesField[1])?.Value.GetValueOrDefault(Calendar.SqlMaxValue))
                              arg.AddError(starkov.Faker.ParametersMatchings.Resources.DialogErrorDateFromGreaterDateTo);
                            else if (customType == Constants.Module.CustomType.Numeric &&
                                     Functions.Module.CastToIntegerDialogValue(personalValuesField[0])?.Value.GetValueOrDefault() >
                                     Functions.Module.CastToIntegerDialogValue(personalValuesField[1])?.Value.GetValueOrDefault(int.MaxValue))
                              arg.AddError(starkov.Faker.ParametersMatchings.Resources.DialogErrorValueFromGreaterValueTo);
                          });
      
      propertyNameField.SetOnValueChanged((arg) =>
                                          {
                                            HideDialogControl(ref personalValuesField);
                                            
                                            if (string.IsNullOrEmpty(arg.NewValue))
                                            {
                                              parameterField.From(Array.Empty<string>());
                                              parameterField.IsEnabled = false;
                                            }
                                            else if (arg.NewValue != arg.OldValue)
                                            {
                                              var selectedPropInfo = propInfo.FirstOrDefault(i => i.Name == propertyNameField.Value);
                                              var parameters = new List<string>();
                                              if (selectedPropInfo != null)
                                                parameters = Functions.ParametersMatching.GetMatchingTypeToParameters(selectedPropInfo.Type) ?? parameters;
                                              
                                              parameterField.From(parameters.ToArray());
                                              parameterField.IsEnabled = true;
                                              
                                              localizedValuesField.Value = propInfo.FirstOrDefault(i => i.Name == arg.NewValue)?.LocalizedName;
                                            }
                                          });
      
      localizedValuesField.SetOnValueChanged((arg) =>
                                             {
                                               if (!string.IsNullOrEmpty(arg.NewValue) && arg.NewValue != arg.OldValue)
                                                 propertyNameField.Value = propInfo.FirstOrDefault(i => i.LocalizedName == arg.NewValue)?.Name;
                                             });
      
      isLocalizedValues.SetOnValueChanged((arg) =>
                                          {
                                            if (arg.NewValue.GetValueOrDefault())
                                            {
                                              localizedValuesField.IsRequired = true;
                                              localizedValuesField.IsEnabled = true;
                                              propertyNameField.IsRequired = false;
                                              propertyNameField.IsEnabled = false;
                                            }
                                            else
                                            {
                                              propertyNameField.IsRequired = true;
                                              propertyNameField.IsEnabled = true;
                                              localizedValuesField.IsRequired = false;
                                              localizedValuesField.IsEnabled = false;
                                            }
                                            
                                            propertyNameField.Value = null;
                                            localizedValuesField.Value = null;
                                          });
      
      parameterField.SetOnValueChanged((arg) =>
                                       {
                                         HideDialogControl(ref personalValuesField);
                                         
                                         var selectedPropInfo = propInfo.FirstOrDefault(i => i.Name == propertyNameField.Value);
                                         if (selectedPropInfo == null)
                                           return;
                                         
                                         ShowDialogControlsByParameter(dialog, arg.NewValue, selectedPropInfo, ref personalValuesField);
                                         
                                         if (isFillValue && !string.IsNullOrEmpty(arg.OldValue))
                                           isFillValue = false;
                                         if (!isFillValue)
                                         {
                                           isFillValue = !personalValuesField.Any();
                                           HideDialogControl(ref personalValuesField);
                                         }
                                       });
      #endregion
      
      #region Заполнение данных
      if (isLocalizedValues.Value != isLocalizedValues.IsVisible)
        isLocalizedValues.Value = isLocalizedValues.IsVisible;
      
      if (parameterRow != null)
      {
        var selectedPropInfo = propInfo.FirstOrDefault();
        propertyNameField.Value = selectedPropInfo.Name;
        propertyNameField.IsEnabled = false;
        localizedValuesField.IsEnabled = false;
        
        if (!string.IsNullOrEmpty(parameterRow.FillOption))
        {
          parameterField.Value = parameterRow.FillOption;
          FillDialogControlFromTable(parameterRow, ref personalValuesField);
        }
      }
      
      if (!isUnique)
      {
        propertyNameField.IsRequired = true;
        localizedValuesField.IsEnabled = false;
      }
      #endregion
      
      #region Кнопки диалога
      if (dialog.Show() == DialogButtons.Ok)
      {
        var selectedPropInfo = propInfo.FirstOrDefault(i => i.Name == propertyNameField.Value);
        
        var newRow = rowId.HasValue ? parameterRow : 
          isChange ? _obj.Parameters.FirstOrDefault(p => p.PropertyName == propertyNameField.Value) : _obj.Parameters.AddNew();
        newRow.PropertyName = selectedPropInfo.Name;
        newRow.LocalizedPropertyName = selectedPropInfo.LocalizedName;
        newRow.PropertyType = Functions.ParametersMatching.GetMatchingTypeToCustomType(selectedPropInfo.Type);
        newRow.PropertyTypeGuid = selectedPropInfo.PropertyGuid;
        newRow.StringPropLength = selectedPropInfo.MaxStringLength;
        newRow.FillOption = parameterField.Value;
        
        newRow.ChosenValue = null;
        newRow.ValueFrom = null;
        newRow.ValueTo = null;
        
        if (!isFillValue)
          ShowDialogForSelectParameters(newRow.Id, true);
        
        else if (personalValuesField.Count == 1)
          newRow.ChosenValue = GetValueFromDialogControl(personalValuesField[0],
                                                         newRow.PropertyType,
                                                         newRow.PropertyTypeGuid);
        else if (personalValuesField.Count == 2)
        {
          newRow.ValueFrom = GetValueFromDialogControl(personalValuesField[0],
                                                       newRow.PropertyType,
                                                       newRow.PropertyTypeGuid);
          newRow.ValueTo = GetValueFromDialogControl(personalValuesField[1],
                                                     newRow.PropertyType,
                                                     newRow.PropertyTypeGuid);
        }
      }
      else if (!string.IsNullOrEmpty(parameterRow?.FillOption) &&
               IsNeedSelectValue(parameterRow.FillOption) &&
               string.IsNullOrEmpty(parameterRow.ChosenValue) &&
               string.IsNullOrEmpty(parameterRow.ValueFrom) &&
               string.IsNullOrEmpty(parameterRow.ValueTo) &&
               isFillValue)
        parameterRow.FillOption = string.Empty;
      #endregion
    }

    #region Работа с контролами диалога
    
    /// <summary>
    /// Проверка на необходимость выбора значения для варианта заполнения.
    /// </summary>
    /// <param name="selectedValue">Вариант заполнения.</param>
    public virtual bool IsNeedSelectValue(string fillOption)
    {
      var options = new List<string>() {
        Constants.Module.FillOptions.Common.FixedValue,
        Constants.Module.FillOptions.Date.Period,
        Constants.Module.FillOptions.Numeric.NumberWithLength,
        Constants.Module.FillOptions.Numeric.NumberRange,
        Constants.Module.FillOptions.String.FirstName,
        Constants.Module.FillOptions.String.LastName,
        Constants.Module.FillOptions.String.FullName
      };
      return options.Contains(fillOption);
    }
    
    /// <summary>
    /// Скрыть контролы диалога.
    /// </summary>
    /// <param name="controls">Контролы.</param>
    private void HideDialogControl(ref List<object> controls)
    {
      foreach (var control in controls)
      {
        var castedControl = Functions.Module.CastToDialogControl(control);
        if (castedControl == null)
          continue;
        
        castedControl.IsVisible = false;
        castedControl.IsRequired = false;
      }
      controls.Clear();
    }
    
    /// <summary>
    /// Вывод контролов в соответствии с вариантом заполнения.
    /// </summary>
    /// <param name="dialog">Диалог.</param>
    /// <param name="selectedValue">Выбранное значение.</param>
    /// <param name="selectedPropInfo">Структура с информацией о свойстве.</param>
    /// <param name="personalValuesField">Список контролов.</param>
    public virtual void ShowDialogControlsByParameter(CommonLibrary.IInputDialog dialog,
                                                      string selectedValue,
                                                      Faker.Structures.Module.PropertyInfo selectedPropInfo,
                                                      ref List<object> personalValuesField)
    {
      if (selectedValue == Constants.Module.FillOptions.Common.FixedValue)
      {
        var customType = Functions.ParametersMatching.GetMatchingTypeToCustomType(selectedPropInfo.Type);
        if (customType == Constants.Module.CustomType.Date)
          personalValuesField.Add(dialog.AddDate(starkov.Faker.ParametersMatchings.Resources.DialogFieldDate, true));
        else if (customType == Constants.Module.CustomType.Bool)
          personalValuesField.Add(dialog.AddBoolean(starkov.Faker.ParametersMatchings.Resources.DialogFieldBooleanValue, true));
        else if (customType == Constants.Module.CustomType.Numeric)
          personalValuesField.Add(dialog.AddInteger(starkov.Faker.ParametersMatchings.Resources.DialogFieldNumber, true));
        else if (customType == Constants.Module.CustomType.String)
          personalValuesField.Add(dialog.AddString(starkov.Faker.ParametersMatchings.Resources.DialogFieldString, true));
        else if (customType == Constants.Module.CustomType.Enumeration)
          personalValuesField.Add(dialog.AddSelect(starkov.Faker.ParametersMatchings.Resources.DialogFieldEnumeration, true)
                                  .From(selectedPropInfo.EnumCollection.Select(i => i.LocalizedName).ToArray()));
        else
          personalValuesField.Add(dialog.AddSelect(starkov.Faker.ParametersMatchings.Resources.DialogFieldValue, true)
                                  .From(Functions.Module.GetEntitiyNamesByType(selectedPropInfo.PropertyGuid,
                                                                               _obj.DocumentType?.DocumentTypeGuid).ToArray()));
      }
      else if (selectedValue == Constants.Module.FillOptions.Date.Period)
      {
        personalValuesField.AddRange(new List<IDateDialogValue>() {
                                       dialog.AddDate(starkov.Faker.ParametersMatchings.Resources.DialogFieldDateFrom, true),
                                       dialog.AddDate(starkov.Faker.ParametersMatchings.Resources.DialogFieldDateTo, true)
                                     });
      }
      else if (selectedValue == Constants.Module.FillOptions.Numeric.NumberWithLength)
      {
        personalValuesField.AddRange(new List<IIntegerDialogValue>() {
                                       dialog.AddInteger(starkov.Faker.ParametersMatchings.Resources.DialogFieldNumberLength, true)
                                     });
      }
      else if (selectedValue == Constants.Module.FillOptions.Numeric.NumberRange)
      {
        personalValuesField.AddRange(new List<IIntegerDialogValue>() {
                                       dialog.AddInteger(starkov.Faker.ParametersMatchings.Resources.DialogFieldFrom, true),
                                       dialog.AddInteger(starkov.Faker.ParametersMatchings.Resources.DialogFieldBy, true)
                                     });
      }
      else if (selectedValue == Constants.Module.FillOptions.String.FirstName ||
               selectedValue == Constants.Module.FillOptions.String.LastName ||
               selectedValue == Constants.Module.FillOptions.String.FullName)
      {
        personalValuesField.AddRange(new List<IDropDownDialogValue>() {
                                       dialog.AddSelect(starkov.Faker.ParametersMatchings.Resources.DialogFieldSex, false)
                                         .From(Enum.GetNames(typeof(Bogus.DataSets.Name.Gender)))
                                     });
      }
    }
    
    /// <summary>
    /// Заполнить генерируемые контролы диалога значениями из таблицы.
    /// </summary>
    /// <param name="parameterRow">Строка с параметрами.</param>
    /// <param name="controls">Контролы.</param>
    public virtual void FillDialogControlFromTable(Faker.IParametersMatchingParameters parameterRow, ref List<object> controls)
    {
      if (!string.IsNullOrEmpty(parameterRow.ChosenValue))
      {
        var controlType = Functions.ParametersMatching.GetMatchingControlTypeToCustomType(parameterRow.PropertyType, controls[0]);
        controls[0].GetType().GetProperty(Constants.Module.PropertyNames.Value).SetValue(controls[0], GetValueInSelectedType(controlType, parameterRow.PropertyTypeGuid, parameterRow.ChosenValue));
      }
      else if (!string.IsNullOrEmpty(parameterRow.ValueFrom) && !string.IsNullOrEmpty(parameterRow.ValueTo))
      {
        var controlType = Functions.ParametersMatching.GetMatchingControlTypeToCustomType(parameterRow.PropertyType, controls[0]);
        controls[0].GetType().GetProperty(Constants.Module.PropertyNames.Value).SetValue(controls[0], GetValueInSelectedType(controlType, parameterRow.PropertyTypeGuid, parameterRow.ValueFrom));
        controls[1].GetType().GetProperty(Constants.Module.PropertyNames.Value).SetValue(controls[1], GetValueInSelectedType(controlType, parameterRow.PropertyTypeGuid, parameterRow.ValueTo));
      }
    }
    
    /// <summary>
    /// Получить значение в указанном типе.
    /// </summary>
    /// <param name="customType">Обобщенный тип.</param>
    /// <param name="typeGuid">Guid типа сущности.</param>
    /// <param name="convertedValue">Значение которое нужно преобразовать.</param>
    /// <returns>Преобразованное значение.</returns>
    public virtual object GetValueInSelectedType(string customType, string typeGuid, string convertedValue)
    {
      DateTime date;
      bool logic;
      int num;
      object result = null;
      
      if (customType == Constants.Module.CustomType.Date && Calendar.TryParseDate(convertedValue, out date))
        result = date;
      else if (customType == Constants.Module.CustomType.Bool && bool.TryParse(convertedValue, out logic))
        result = logic;
      else if (customType == Constants.Module.CustomType.Numeric && int.TryParse(convertedValue, out num))
        result = num;
      else if (customType == Constants.Module.CustomType.Navigation)
        result = Functions.Module.GetEntitiyNamesByType(typeGuid, _obj.DocumentType?.DocumentTypeGuid).FirstOrDefault(x => x == convertedValue);
      else
        result = convertedValue;
      
      return result;
    }
    
    /// <summary>
    /// Получить значение из контрола диалога в виде строки.
    /// </summary>
    /// <param name="control">Контрол.</param>
    /// <param name="customType">Обобщенный тип.</param>
    /// <param name="typeGuid">Guid типа сущности.</param>
    /// <returns>Значение из контрола в виде строки.</returns>
    public virtual string GetValueFromDialogControl(object control, string customType, string typeGuid)
    {
      var result = string.Empty;
      if (control == null)
        return result;
      
      if (customType == Constants.Module.CustomType.Enumeration || customType == Constants.Module.CustomType.Navigation)
        result = Functions.Module.CastToDialogControlString(control)?.Value;
      else if (Functions.Module.CompareObjectWithType(control, typeof(Sungero.WebAPI.Dialogs.DateDialogControl)))
        result = Functions.Module.CastToDateDialogValue(control)?.Value.GetValueOrDefault().ToShortDateString();
      else if (Functions.Module.CompareObjectWithType(control, typeof(Sungero.WebAPI.Dialogs.BooleanDialogControl)))
        result = Functions.Module.CastToBooleanDialogValue(control)?.Value.GetValueOrDefault().ToString();
      else if (Functions.Module.CompareObjectWithType(control, typeof(Sungero.WebAPI.Dialogs.IntegerDialogControl)))
        result = Functions.Module.CastToIntegerDialogValue(control)?.Value.GetValueOrDefault().ToString();
      else if (Functions.Module.CompareObjectWithType(control, typeof(Sungero.WebAPI.Dialogs.StringDialogControl)))
        result = Functions.Module.CastToStringDialogValue(control)?.Value;
      else if (Functions.Module.CompareObjectWithType(control, typeof(Sungero.WebAPI.Dialogs.DropDownDialogControl)))
        result = Functions.Module.CastToDropDownDialogValue(control)?.Value;
      
      return result ?? string.Empty;
    }
    
    /// <summary>
    /// Получить обобщенный тип по типу контрола.
    /// </summary>
    /// <param name="customType">Обобщенное наименование типа свойства.</param>
    /// <param name="control">Контрол.</param>
    /// <returns>Обобщенное наименование типа.</returns>
    public static string GetMatchingControlTypeToCustomType(string customType, object control)
    {
      if (customType == Constants.Module.CustomType.Enumeration || customType == Constants.Module.CustomType.Navigation)
        return customType;
      
      if (Functions.Module.CompareObjectWithType(control, typeof(Sungero.WebAPI.Dialogs.DateDialogControl)))
        customType = Constants.Module.CustomType.Date;
      else if (Functions.Module.CompareObjectWithType(control, typeof(Sungero.WebAPI.Dialogs.BooleanDialogControl)))
        customType = Constants.Module.CustomType.Bool;
      else if (Functions.Module.CompareObjectWithType(control, typeof(Sungero.WebAPI.Dialogs.IntegerDialogControl)))
        customType = Constants.Module.CustomType.Numeric;
      else if (Functions.Module.CompareObjectWithType(control, typeof(Sungero.WebAPI.Dialogs.StringDialogControl)))
        customType = Constants.Module.CustomType.String;
      
      return customType;
    }
    
    #endregion
  }
}