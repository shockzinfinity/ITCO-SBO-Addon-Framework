﻿using SAPbobsCOM;
using System;
using System.Collections.Generic;

namespace ITCO.SboAddon.Framework.Helpers
{
    /// <summary>
    /// Helper for creating UD objects
    /// </summary>
    public static class UserDefinedHelper
    {
        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<string, string> YesNoValiesValues => new Dictionary<string, string>
        {
            { "Y", "Yes"},
            { "N", "No" }
        };

        /// <summary>
        /// User defined table object
        /// </summary>
        public class UserDefinedTable
        {
            public UserDefinedTable(string tableName)
            {
                TableName = tableName;
            }
            public string TableName { get; set; }

            // Floud API
            public UserDefinedTable CreateUDF(string fieldName, string fieldDescription,
            BoFieldTypes type = BoFieldTypes.db_Alpha, int size = 50, BoFldSubTypes subType = BoFldSubTypes.st_None,
            IDictionary<string, string> validValues = null, string defaultValue = null)
            {
                CreateField(TableName, fieldName, fieldDescription, type, size, subType, validValues, defaultValue);            
                return this;
            }
        }

        /// <summary>
        /// Create UDT
        /// </summary>
        /// <param name="tableName">Table name eg: NS_MyTable</param>
        /// <param name="tableDescription"></param>
        /// <param name="tableType"></param>
        /// <returns>Success</returns>
        public static UserDefinedTable CreateTable(string tableName, string tableDescription, BoUTBTableType tableType = BoUTBTableType.bott_NoObject)
        {
            UserTablesMD userTablesMd = null;
            
            try
            {
                userTablesMd = SboApp.Company.GetBusinessObject(BoObjectTypes.oUserTables) as UserTablesMD;

                if (userTablesMd == null)
                    throw new NullReferenceException("Failed to get UserTablesMD object");

                if (!userTablesMd.GetByKey(tableName))
                {
                    userTablesMd.TableName = tableName;
                    userTablesMd.TableDescription = tableDescription;
                    userTablesMd.TableType = tableType;

                    ErrorHelper.HandleErrorWithException(
                        userTablesMd.Add(),
                        $"Could not create UDT {tableName}");
                }
            }
            catch (Exception ex)
            {
                SboApp.Logger.Error($"UDT Create Error: {ex.Message}", ex);
                throw;                
            }
            finally
            {
                if (userTablesMd != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(userTablesMd);
            }

            return new UserDefinedTable("@" + tableName);
        }

        /// <summary>
        /// Create UDF on UDT
        /// </summary>
        /// <param name="tableName">UDT Name without @</param>
        /// <param name="fieldName">Field name</param>
        /// <param name="fieldDescription"></param>
        /// <param name="type">BoFieldTypes type</param>
        /// <param name="size"></param>
        /// <param name="subType"></param>
        /// <returns></returns>
        public static void CreateFieldOnUDT(string tableName, string fieldName, string fieldDescription, 
            BoFieldTypes type = BoFieldTypes.db_Alpha, int size = 50, BoFldSubTypes subType = BoFldSubTypes.st_None)
        {
            tableName = "@" + tableName;
            CreateField(tableName, fieldName, fieldDescription, type, size, subType);
        }

        /// <summary>
        /// Create field on table
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="fieldDescription"></param>
        /// <param name="type"></param>
        /// <param name="size"></param>
        /// <param name="subType"></param>
        /// <param name="validValues">Dropdown values</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static void CreateField(string tableName, string fieldName, string fieldDescription, 
            BoFieldTypes type = BoFieldTypes.db_Alpha, int size = 50, BoFldSubTypes subType = BoFldSubTypes.st_None, 
            IDictionary<string, string> validValues = null, string defaultValue = null)
        {
            UserFieldsMD userFieldsMd = null;

            try
            {
                userFieldsMd = SboApp.Company.GetBusinessObject(BoObjectTypes.oUserFields) as UserFieldsMD;

                if (userFieldsMd == null)
                    throw new NullReferenceException("Failed to get UserFieldsMD object");

                var fieldId = GetFieldId(tableName, fieldName);
                if (fieldId != -1) return;

                userFieldsMd.TableName = tableName;
                userFieldsMd.Name = fieldName;
                userFieldsMd.Description = fieldDescription;
                userFieldsMd.Type = type;
                userFieldsMd.SubType = subType;
                userFieldsMd.Size = size;
                userFieldsMd.EditSize = size;
                userFieldsMd.DefaultValue = defaultValue;

                if (validValues != null)
                {
                    foreach (var validValue in validValues)
                    {
                        userFieldsMd.ValidValues.Value = validValue.Key;
                        userFieldsMd.ValidValues.Description = validValue.Value;
                        userFieldsMd.ValidValues.Add();
                    }
                }

                ErrorHelper.HandleErrorWithException(userFieldsMd.Add(), "Could not create field");
            }
            catch (Exception ex)
            {
                SboApp.Logger.Error($"Create Field {tableName}.{fieldName} Error: {ex.Message}", ex);
                throw;
            }
            finally
            {
                if (userFieldsMd != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(userFieldsMd);
            }
        }

        /// <summary>
        /// Get Field Id
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldAlias"></param>
        /// <returns></returns>
        public static int GetFieldId(string tableName, string fieldAlias)
        {
            var recordSet = SboApp.Company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;

            if (recordSet == null)
                throw new NullReferenceException("Failed to get Recordset object");

            try
            {
                recordSet.DoQuery($"SELECT FieldID FROM CUFD WHERE TableID='{tableName}' AND AliasID='{fieldAlias}'");

                if (recordSet.RecordCount == 1)
                {
                    var fieldId = recordSet.Fields.Item("FieldID").Value as int?;
                    return fieldId.Value;
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(recordSet);
            }
            return -1;
        }
    }
}
