var validation = {
  isNotEmpty: function(str) {
    var pattern = /\S+/;
    return pattern.test(str);
  },
  isNumber: function(str) {
    var pattern = /^\d+$/;
    return pattern.test(str);
  },
  isSame: function(str1, str2) {
    return str1 === str2;
  }
};

function TableGen() {
  var col = [];

  var rowKey = [];

  var tbody;

  var _tableID;

  this.CreateTableFromJSON = function(id, myTable, tableID) {
    col = [];
    rowKey = [];

    _tableID = tableID;

    var nameFh = "#" + _tableID + " thead tr th";
    var heads = $(nameFh);

    for (var i = 0; i < heads.length - 1; i++) {
      var key = $(heads[i]).text();
      col.push(key);
    }



    var nameF = "#" + _tableID + " tbody";
    tbody = $(nameF);

    for (var i2 = 0; i2 < myTable.length; i2++) {
      var rowData = myTable[i2];
      this.AddTableRow(i2, rowData);
    }

  };

  this.AddTableRow = function(i2, rowData) {
    var tr = $('<tr>');
    tbody.append(tr)

    var k = i2 % 2;

    if (k === 1) {
      tr.addClass( "tr_row2");
    } else {
      tr.addClass( "tr_row1");
    }

    for (var j = 0; j < col.length; j++) {
      var tabCell = $('<td>');
      tr.append(tabCell)


      var st = rowData[col[j]];

      if (j == 0) {
        var cleanedId = "ID_" + st.replace(/([^A-Za-z0-9[\]{}_.:-])\s?/g, "_");
        rowKey.push(st);
        tr.attr("id", cleanedId);
      }
          $(tabCell).text(st);
    }
    var tabCell1 = $('<td>');
    tr.append(tabCell1)

    var img = $("<img>");

    img.attr("class", "del_button");
    img.attr("src","/js/table/themes/delete.png");
    $(img).click(function(e) {
      var t = $(this).parent().parent();
      var key = $($(t).children()[0]).text();

      deleteKeyRow(key);
    });

    tabCell1.append(img);
  };

  this.removeID = function(element) {
    const index = rowKey.indexOf(element);

    if (index !== -1) {
      rowKey.splice(index, 1);
    }
  };

  this.insertRow = function(rowData) {
    var Rkey = rowData["dataKey"];

    if (rowKey.indexOf(Rkey) === -1) {
      var nameF = "#" + _tableID + " tbody tr";
      var rows = $(nameF);
      var i2 = $(nameF).length;
      this.AddTableRow(i2, rowData);
    } else {
      var dValue = rowData["dataValue"];
      if (dValue === "") {
        this.removeID(Rkey);
        var cleanedId =
          "ID_" + Rkey.replace(/([^A-Za-z0-9[\]{}_.:-])\s?/g, "_");
        var id_row = "#" + cleanedId;
        $(id_row).remove();
      }
    }
  };
}
