﻿/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System.Data;

namespace OnTopic.Data.Sql.Models {

  /*============================================================================================================================
  | CLASS: ATTRIBUTE VALUES (DATA TABLE)
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   Extends <see cref="DataTable"/> to model the schema for the <c>AttributeValues</c> user-defined table type.
  /// </summary>
  internal class AttributeValuesDataTable: DataTable {

    /*==========================================================================================================================
    | CONSTRUCTOR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Establishes a new <see cref="DataTable"/> with the appropriate schema for the <c>AttributeValues</c> user-defined
    ///   table type.
    /// </summary>
    internal AttributeValuesDataTable() {

      /*------------------------------------------------------------------------------------------------------------------------
      | COLUMN: Attribute Key
      \-----------------------------------------------------------------------------------------------------------------------*/
      Columns.Add(
        new DataColumn("AttributeKey") {
          MaxLength             = 128
        }
      );

      /*------------------------------------------------------------------------------------------------------------------------
      | COLUMN: Attribute Value
      \-----------------------------------------------------------------------------------------------------------------------*/
      Columns.Add(
        new DataColumn("AttributeValue") {
          MaxLength             = 255
        }
      );

    }

    /*==========================================================================================================================
    | ADD ROW
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Provides a convenience method for adding a new <see cref="DataRow"/> based on the expected column values.
    /// </summary>
    internal DataRow AddRow(string attributeKey, string? attributeValue = null) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Define record
      \-----------------------------------------------------------------------------------------------------------------------*/
      var record                = NewRow();
      record["AttributeKey"]    = attributeKey;
      record["AttributeValue"]  = attributeValue;

      /*------------------------------------------------------------------------------------------------------------------------
      | Add record
      \-----------------------------------------------------------------------------------------------------------------------*/
      Rows.Add(record);

      /*------------------------------------------------------------------------------------------------------------------------
      | Return record
      \-----------------------------------------------------------------------------------------------------------------------*/
      return record;

    }

  } //Class
} //Namespaces