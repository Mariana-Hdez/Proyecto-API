function getSales() {
    const start = document.getElementById('start').value;
    const end = document.getElementById('end').value;
    const bodyPost = {
        startDate: start,
        endDate: end
    }
    google.charts.load('current', { 'packages': ['corechart', 'controls'] });
    google.charts.setOnLoadCallback(() => {
        fetch("/Product/GetPeriodo/" + start + "/" + end)
            .then(response => {
                if (!response.ok) {
                    throw response;
                }
                return response.json();
            })
            .then(info => {
                cargarGraficaVentas(info);
                console.log(info)
            })
            .catch(error => console.log(error));
    });
}

function cargarGraficaVentas(info) {
    var data1 = new google.visualization.DataTable();
    data1.addColumn('number', 'Mes');
    data1.addColumn('number', 'Ventas');

    var arrDatos = [];

    arrDatos.push(['Mes', 'Ventas']);

    info.forEach(f => {
        data1.addRow([f.month, f.totalPrice]);
        arrDatos.push([getMonthName(f.month), f.totalPrice]);
    })
    var data2 = google.visualization.arrayToDataTable(arrDatos);
    console.log(data2)
    var options = {
        title: 'Ventas por Mes (Gráfica de Barras)',
        crosshair: { trigger: 'both' },
        legend: { position: 'top' },
        hAxis: {
            title: 'Año con el mes',
            format: 'none',
            showTextEvery: 1,
            slantedTextAngle: '45'
        },
        textStyle: { fontSize: 10 },
        chartArea: {
            width: '90%', left: '100', bottom: '200', heigth: '40%'
        },
        isStacked: true
    };



    var chart = new google.visualization.ColumnChart(document.getElementById('ValorVentas'));

    chart.draw(data2, options);

    tabla();

}

function getMonthName(monthNumber) {
    const date = new Date();
    date.setMonth(monthNumber - 1);

    return date.toLocaleString('en-US', { month: 'long' });
}

function tabla() {

    var start = document.getElementById('start');
    var end = document.getElementById('end');

    var tabla;
    var start = start.value;
    var end = end.value;

    // Para la solicitud fetch
    var url = "/Product/GetPeriodo/" + start + "/" + end;

    fetch(url)
        .then(response => response.json())
        .then(info => {
            var idTabla = '#tblProductos';

            // Verificar si la DataTable ya existe y destruirla
            if (tabla && $.fn.DataTable.isDataTable(idTabla)) {
                tabla.destroy();
            }

            // Crear instancia de la tabla
            tabla = $(idTabla).DataTable({
                buttons: [
                    'copy',
                    'csv',
                    'excel',
                    'pdf',
                    'print',
                    'colvis'
                ],
                data: info,
                columns: [
                    {
                        title: 'Año', data: 'year', render: $.fn.dataTable.render.text()
                    },
                    {
                        title: 'Mes', data: 'month', render: $.fn.dataTable.render.number()
                    },
                    {
                        title: 'Ventas totales', data: 'totalPrice', render: $.fn.dataTable.render.number()
                    }
                ],
                "fnRowCallback": function (nRow, aData, iDisplayIndex) {
                    // Añadir estilo a una fila o columna dependiendo de algún valor
                },
                "fnInitComplete": function (oSettings, json) {
                    // Configuración de los filtros individuales
                },
                // Configuraciones adicionales
                dom: 'Bfrtip',
                colReorder: true,
                responsive: true,
                order: [[1, 'asc']],
                rowGroup: {
                    startRender: null,
                    endRender: function (rows, group) {
                        var sum = rows
                            .data()
                            .pluck('unidadesVendidas')
                            .reduce(function (a, b) {
                                return a + b;
                            }, 0);
                        return 'Productos Vendidos Trimestre ' + group + ': ' + sum;
                    },
                    dataSrc: 'trimestre'
                },
                lengthMenu: [[25, 50, 100, -1], [25, 50, 100, "All"]]
            });

            // Mover los botones de exportación al lugar correcto
            tabla.buttons().container().appendTo('#tblProductos_wrapper .row:eq(0) .col-md-6:eq(0)');
        })
        .catch(error => console.log(error));
}
