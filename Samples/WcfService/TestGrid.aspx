<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TestGrid.aspx.cs" Inherits="WcfService.TestGrid" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>

    <style type="text/css">
        .gridStyle {
            border: 1px solid rgb(212,212,212);
            width: 400px; 
            height: 300px;
            float: left;
        }

        .selectedItems{
            float: left; 
        }        
    </style>

</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div class="gridStyle" data-bind="koGrid: gridOptions"></div>
        <div class="selectedItems" data-bind="with: mySelections()[0]">
            <p data-bind="text: name"></p>
        </div>    
    </div>
    </form>
</body>
</html>
