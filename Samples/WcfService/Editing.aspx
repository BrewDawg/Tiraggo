<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Editing.aspx.cs" Inherits="Tiraggo_js.Editing" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="head" runat="server">

    <title>Tiraggo.js Editing Sample</title>
    <script src="Scripts/Libs/jquery-1.10.2.min.js" type="text/javascript"></script>
    <script src="Scripts/Libs/json2.js" type="text/javascript"></script>
    <script src="Scripts/Knockout/knockout-3.0.0.debug.js" type="text/javascript"></script>
    <link href="Scripts/KoGrid/KoGrid.css" rel="stylesheet" type="text/css" />
    <script src="Scripts/KoGrid/koGrid.debug.js" type="text/javascript"></script>

    <script src="Scripts/tiraggo.XHR.debug.js" type="text/javascript"></script>   
    <script src="Tiraggo_js/Generated/Employees.js" type="text/javascript"></script>

    <style type="text/css">
        .gridStyle {
            border: 1px solid rgb(212,212,212);
            width: 760px; 
            height: 300px;
            float: left;
        }
    </style>

</head>
<body>
    <form id="form1" runat="server">
    <div>
        <table>
            <tr>
                <td colspan="3">
                    The Editing <a href="http://brewdawg.github.com/Tiraggo.js/" target="new">Tiraggo.js</a> demonstration for the <a href="http://brewdawg.github.com/Tiraggo/" target="new">Tiraggo</a> Architecture. Everything need to build this sample was generated on <a href="https://www.my2ndgeneration.com/" target="new">My2ndGeneration</a><br />
                    <ul>
                        <li>The <a href="https://github.com/BrewDawg/Tiraggo/blob/master/Samples/WcfService/Tiraggo/Generated/Employees.cs" target="new">Generated</a> and <a href="https://github.com/BrewDawg/Tiraggo/tree/master/Samples/WcfService/Tiraggo/Custom" target="new">Custom</a> Classes</li>
                        <li>The <a href="https://github.com/BrewDawg/Tiraggo/blob/master/Samples/WcfService/TiraggoWcfClass.svc.cs" target="new">WCF Service</a> (returns JSON)</li>
                        <li>The <a href="https://github.com/BrewDawg/Tiraggo/blob/master/Samples/WcfService/Tiraggo_js/Generated/Employees.js" target="new">JavaScript Entities</a></li>
                        <li>The <a href="https://github.com/BrewDawg/Tiraggo/blob/master/Samples/WcfService/Editing.aspx" target="new">HTML Markup</a> and MVVM View Model</li>
                    </ul>
                </td>
            </tr>
            <tr>
                <td colspan="4">
                    <strong>NOTE:</strong> This is accessing a real database. Some records cannot be
                    deleted because of foreign key constraints. We are showing all of the low level
                    errors that come back from our service just as an example. In real world scenarios
                    you would not do this, of course.
                    <hr />
                </td>
            </tr>
            <tr>
                <td colspan="4" data-bind="text: error" style="color: Red; font-weight: bold;">
                </td>
            </tr>
            <tr>
                <td valign="top" align="left" style="width: 1%;">
                    <div id="myGrid" style="max-height: 300px; max-width: 730px; border: 1px solid rgb(0, 0, 0);"
                        data-bind="koGrid:{ data: collection, 
                            isMultiSelect: false,
                            selectedItem: mySelectedItem,
                            autogenerateColumns: false,
                            sortInfo: sortInfo,
                            enablePaging: false,
                            columnDefs: [
                                {field: 'EmployeeID', displayName: 'ID', width: 50}, 
                                {field: 'FirstName', displayName: 'First Name', width: 120}, 
                                {field: 'LastName', displayName: 'Last Name', width: 160},
                                {field: 'RowState', displayName: 'RowState', width: 90},
                                {field: 'isDirty', displayName: 'isDirty', width: 80},
                                {field: 'ModifiedColumns', displayName: 'ModifiedColumns', width: 160}
                            ],
                            displaySelectionCheckbox: true,
                            displayRowIndex: true }">
                    </div>
                </td>
                <td>
                    &nbsp;&nbsp;
                </td>
                <td valign="top" align="left" rowspan="6">
                    <!--------------------------->
                    <!-- This is our Edit Area -->
                    <!--------------------------->
                    <table id="EditArea" cellpadding="3" width="400px" style="max-width: 400px; vertical-align: top; border: 1px solid rgb(0,0,0);" data-bind="with: mySelectedItem">
                        <tr align="center">
                            <td colspan="2">
                                <b>Edit Selected Employee Here</b>
                                <hr />
                            </td>
                        </tr>
                        <tr align="center">
                            <td colspan="2">
                                The Original Values are stored for each column. They are not shown below. However, they are restored when you selected 'Reject Changes'.
                                <hr />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <strong>EmployeeID:</strong>
                            </td>
                            <td data-bind="text: EmployeeID">
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <strong>First Name:</strong>
                            </td>
                            <td>
                                <input data-bind='value: FirstName, valueUpdate: "afterkeydown"' maxlength="10" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <strong>Last Name:</strong>
                            </td>
                            <td>
                                <input data-bind='value: LastName, valueUpdate: "afterkeydown"' maxlength="20" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <strong>RowState:</strong>
                            </td>
                            <td data-bind="text: RowState">
                            </td>
                        </tr>
                        <tr align="center">
                            <td colspan="2">
                                <hr />
                                RowState:<br /> Unchanged=2, Added=4, Deleted=8, Modified=16
                                <hr />
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2" align="center">
                                <button data-bind="click: $root.onMarkAllAsDeleted" style="width: 280px">
                                    Mark 'All' as Deleted</button>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2" align="center">
                                <button data-bind="click: $root.onMarkAsDeleted" style="width: 280px">
                                    Mark 'Selected Item' as Deleted</button>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2" align="center">
                                <button data-bind="click: $root.onRejectChanges" style="width: 280px">
                                    Reject All Changes</button>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2" align="center">
                                <button data-bind="click: $root.onAddNew" style="width: 280px">
                                    Add a New Employee</button>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2" align="center">
                                <button data-bind="click: $root.onSave" style="width: 280px">
                                    Save changes</button>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr valign="top">
                <td>
                    <br />
                    <b>The Collections Deleted Item List (collection.tg.deletedEntities)</b>
                </td>
            </tr>
            <tr valign="top">
                <td valign="top" align="left" style="width: 1%" rowspan="3">
                    <div id="myDeleted" style="max-height: 200px; max-width: 710px; border: 1px solid rgb(0,0,0);"
                        data-bind="koGrid:{ data: collection.tg.deletedEntities, 
                        isMultiSelect: false,
                        autogenerateColumns: false,
                        sortInfo: sortInfo,
                        columnDefs: [
                            {field: 'EmployeeID', displayName: 'ID', width: 30}, 
                            {field: 'FirstName', displayName: 'First Name', width: 120}, 
                            {field: 'LastName', displayName: 'Last Name', width: 160},
                            {field: 'RowState', displayName: 'RowState', width: 90},
                            {field: 'isDirty', displayName: 'isDirty', width: 80},
                            {field: 'ModifiedColumns', displayName: 'ModifiedColumns', width: 160}
                        ],
                        displaySelectionCheckbox: true,
                        displayRowIndex: true }">
                    </div>
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>

<script language="javascript" type="text/javascript">

    tg.dataProvider.baseURL = "TiraggoWcfClass.svc/";

    $(document).ready(function () {

        var vm = {
            collection: new tg.objects.EmployeesCollection(),
            mySelectedItem: ko.observable(new tg.objects.Employees()),
            error: ko.observable(),
            sortInfo: ko.observable(),

            onAddNew: function () {
                vm.error("");

                //  Add the new Employee
                var newEmployee = vm.collection.addNew();
                newEmployee.FirstName("Joe");
                newEmployee.LastName("Smith");

                vm.mySelectedItem(newEmployee);
            },

            onMarkAllAsDeleted: function () {
                vm.error("");
                vm.collection.markAllAsDeleted();

                vm.mySelectedItem(new tg.objects.Employees());
            },

            onMarkAsDeleted: function () {
                vm.error("");

                var index = ko.utils.arrayIndexOf(vm.collection(), vm.mySelectedItem());

                // only line of entityspaces.js code necessary
                vm.collection.markAsDeleted(vm.mySelectedItem());

                if (vm.collection().length > 0) {
                    vm.mySelectedItem(vm.collection()[Math.max(index, 0)]);
                }
            },

            onRejectChanges: function () {
                vm.error("");
                vm.collection.rejectChanges();
            },

            onSave: function () {
                vm.collection.save({
                    success: function (data, state) {
                        vm.error("");
                    },
                    error: function (status, responseText, state) {
                        vm.error("Ooops !! Something failed ...");
                    }
                });
            }
        };

        ko.applyBindings(vm);

        // Hit our service ...
        vm.collection.loadAll();

        if (vm.collection().length > 0) {
            vm.mySelectedItem(vm.collection()[0]);
        }
    });
</script>
