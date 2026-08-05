using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplicationSampleTest2.Models
{
    public class PriscriptionReport
    {
        public static PatientReportModel Model { get; set; }
        public PriscriptionReport()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }


        public bool GenrateReport(PatientReportModel patientReportModel)
        {
            try
            {

                var document = CreateDocument(patientReportModel);

                //// generate PDF file and return it as a response
                var pdf = document.GeneratePdf();

                System.IO.File.WriteAllBytes(@"D:\\Print\\hello.pdf", pdf);
            }
            catch (Exception ex)
            {
            }

            return false;
        }

        public static QuestPDF.Infrastructure.IDocument CreateDocument(PatientReportModel patientModel)
        {
            Model = patientModel;
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);



                    //page.Header().Element(ComposeHeader);
                    page.Header().Column(column =>
                    {
                        column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
                        var maintitleStyle = TextStyle.Default.FontSize(25).Bold().FontColor(Colors.Blue.Medium);
                        var add = TextStyle.Default.FontSize(15).FontColor(Colors.Blue.Medium);
                        var titleStyle = TextStyle.Default.FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text(Model.Hostpital_Name).Style(maintitleStyle).AlignCenter();
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text(Model.Address).Style(add).AlignCenter();
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                //column.Item().Text($"Invoice #{Model.InvoiceNumber}").Style(titleStyle);

                                column.Item().Text(text =>
                                {
                                    text.Span("Dr. ").SemiBold();
                                    text.Span($"{Model.DrName:d}");
                                });

                                column.Item().Text(text =>
                                {
                                    text.Span("Edu. ").SemiBold();
                                    text.Span($"{Model.DrEducation:d}");
                                });

                                column.Item().Text(text =>
                                {
                                    text.Span("Visiting Time: ").SemiBold();
                                    text.Span($"{Model.VisitTIme:d}");
                                });

                                column.Item().Text(Environment.NewLine);
                                column.Item().Text(Environment.NewLine);

                                column.Item().Text(text =>
                                {
                                    text.Span("Patient Name: ").SemiBold();
                                    text.Span($"{Model.PatientName:d}");
                                });
                                column.Item().Text(text =>
                                {
                                    text.Span("Age: ").SemiBold();
                                    text.Span($"{Model.Age:d}");
                                });
                                column.Item().Text(text =>
                                {
                                    text.Span("BP: ").SemiBold();
                                    text.Span($"{Model.BP:d}");
                                });
                                column.Item().Text(text =>
                                {
                                    text.Span("Pluse: ").SemiBold();
                                    text.Span($"{Model.Pluse:d}");
                                });
                            });

                            row.ConstantItem(120).Height(90).Width(120).Image(@"E:\Application\WebApplicationSampleTest2\WebApplicationSampleTest2\wwwroot\assets\images\hosplital.jpg");


                        });

                        column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);

                    });


                    page.Content().Element(ComposeContent);





                    page.Footer().AlignCenter().Text("Power By OpsTeQ Software Pvt Ltd.");


                });
            });
        }

        private static void ComposeContent(IContainer container)
        {
            IContainer BlockStyle(IContainer container) => container.Background(Colors.White).Padding(10);
            IContainer BlockStyle1(IContainer container) => container.Background(Colors.White).Padding(4);

            container
                .Padding(20)
                .Height(350)
                .Background(Colors.White)
                .DefaultTextStyle(x => x.FontSize(16))
.Column(column =>
{
    column.Item().Text("Investigation : " + Model.Diagnosis).AlignLeft();
    column.Item().Element(BlockStyle).Text("").AlignCenter();
    column.Item().Text("Medicine Details").AlignCenter();
    column.Item().Element(BlockStyle).Text("").AlignCenter();
   

    for (int i = 0; i < Model.tablate.Count; i++)
    {
        var tablates = Model.tablate[i];
        column.Item().Element(BlockStyle1).Text("").AlignCenter();
        column.Item().Text(i + 1 + ". " + tablates.TablateName + "      " + tablates.Morning + "--------------" + tablates.Afternoon + "--------------" + tablates.Evening);
    }
    column.Item().Element(BlockStyle).Text("").AlignCenter();
    column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
  
    column.Item().Element(BlockStyle).Text("Detail Report : " +Model.ReportDetail).Justify();
    

});
            //.AlignCenter()
            //.AlignMiddle()
            //.Text(t => t.Element(ComposeTable));
        }


        private static void ComposeTable(IContainer container)
        {



            //            var pageSizes = new List<(string name, string Morning, string Afternoon, string Evening)>()
            //{
            //    ("Letter (ANSI A)","Y", "","N"),
            //    ("Legal","Y", "",""),
            //    ("Ledger (ANSI B)","Y", "","N"),
            //    ("Tabloid (ANSI B)","Y", "","Y"),
            //    ("ANSI C","Y", "","Y"),
            //    ("ANSI D", "Y", "N",""),
            //    ("ANSI E","Y", "","Y"),
            //};
            //            container.Padding(10)
            //.MinimalBox()
            //.Border(1)
            //.Table(table =>
            //{


            //    table.ColumnsDefinition(columns =>
            //    {
            //        columns.ConstantColumn(150);

            //        columns.ConstantColumn(75);
            //        columns.ConstantColumn(75);

            //        columns.ConstantColumn(75);

            //    });

            //    table.Header(header =>
            //    {
            //        // please be sure to call the 'header' handler!

            //        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignLeft().Text("Tablets");

            //        header.Cell().ColumnSpan(3).Element(CellStyle).Text("Schedule");
            //        header.Cell().Element(CellStyle).Text("Morning");
            //        header.Cell().Element(CellStyle).Text("Afternoon");
            //        header.Cell().Element(CellStyle).Text("Evening");

            //        // you can extend existing styles by creating additional methods
            //        IContainer CellStyle(IContainer container) => DefaultCellStyle(container, Colors.Grey.Lighten3);
            //    });

            //    IContainer DefaultCellStyle(IContainer container, string backgroundColor)
            //    {
            //        return container
            //            .Border(1)
            //            .BorderColor(Colors.Grey.Lighten1)
            //            .Background(backgroundColor)
            //            .PaddingVertical(5)
            //            .PaddingHorizontal(10)
            //            .AlignCenter()
            //            .AlignMiddle();
            //    }



            //    foreach (var page in pageSizes)
            //    {
            //        table.Cell().Element(CellStyle).ExtendHorizontal().AlignLeft().Text(page.name);

            //        // inches
            //        table.Cell().Element(CellStyle).Text(page.Morning);
            //        table.Cell().Element(CellStyle).Text(page.Afternoon);
            //        table.Cell().Element(CellStyle).Text(page.Evening);

            //        IContainer CellStyle(IContainer container) => DefaultCellStyle(container, Colors.White).ShowOnce();
            //    }

            //});


        }

    }
}
