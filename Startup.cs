using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Controllers;
using WebApplicationSampleTest2.Filters;
using WebApplicationSampleTest2.Repository;
using static WebApplicationSampleTest2.Repository.BedService;

namespace WebApplicationSampleTest2
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

public IConfiguration Configuration { get; }

        // ── SECRETS NOTE ───────────────────────────────────────────────
        // appsettings.json intentionally does NOT contain real DB/email
        // passwords. Credentials are provided at runtime by (in order of
        // precedence):
        //   1. Environment variables (ConnectionStrings__MySqlConnection,
        //      EmailSettings__Password, etc. — ASP.NET Core maps the
        //      double-underscore to a ':' section separator automatically).
        //   2. appsettings.Development.json (gitignored) for local dev.
        //      Copy the real DB/email credentials there.
        // This keeps secrets out of source control while the app still runs
        // locally and in production.

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
// NOTE: Single registration only. Adding AddControllersWithViews() twice
            // (once here + once with AddNewtonsoftJson) re-registers MVC and adds
            // duplicate metadata providers / overhead on every request. The
            // NewtonsoftJson overload keeps JSON serialization working exactly as before.
            services.AddControllersWithViews(options =>
            {
                // ── GLOBAL AUTHENTICATION (SECURITY) ─────────────────────
                // Previously only DailyNotesController required a login, so
                // every other controller was reachable by anyone with the URL.
                // Registering the filter globally protects all controllers
                // except the explicit anonymous allowlist (login/registration,
                // home, patient portal pre-login AJAX) defined inside the filter.
                options.Filters.Add(new RequireLoginGlobalFilter());
            })
                    .AddNewtonsoftJson()
                    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

            // GZip-compress HTML/JSON responses — big win on slow connections.
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
            });
            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

            // Simple in-memory cache (used by dashboard counts later if needed).
            services.AddMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(12000);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<Ipatient, PatientService>();
            services.AddScoped<ISymptom, SymptomRepository>();
            services.AddScoped<IMedicine, MedicineRepository>();
            services.AddScoped<IHospital, HospitalRepository>();
            services.AddScoped<IBillingMaster, BillingMasterRepository>();
            services.AddScoped<IUser, UserRepository>();
            services.AddScoped<IDoctor, DoctorRepository>();
            services.AddScoped<IOPDAppointment, OPDAppointmentRepository>();
            services.AddScoped<IOPD, OPDRepository>();
            services.AddScoped<IWard, WardService>();
            services.AddScoped<IRoom, RoomService>();
            services.AddScoped<IBed, BedRepository>();
            services.AddScoped<IIPDAdmission, IPDAdmissionRepository>();
            services.AddScoped<IReferenceDoctor, ReferenceDoctorRepository>();
            services.AddScoped<IIPDBedAllocation, IPDBedAllocationRepository>();
            services.AddScoped<INurse, NurseRepository>();
            services.AddScoped<IIPDNurseVitals, IPDNurseVitalsRepository>();
            services.AddScoped<IDoctorRound, DoctorRoundRepository>();
            services.AddScoped<ILabInvestigation, LabInvestigationRepository>();
            services.AddScoped<IDischarge, DischargeRepository>();
            services.AddScoped<IIPDBilling, IPDBillingRepository>();
            services.AddScoped<IOperationMaster, OperationMasterRepository>();
            services.AddScoped<IIPDOperation, IPDOperationRepository>();
            services.AddScoped<INursingChargesMaster, NursingChargesMasterRepository>();
            services.AddScoped<IIPDNursingCharge, IPDNursingChargeRepository>();
            services.AddScoped<IOPDBilling, OPDBillingRepository>();
            services.AddScoped<IPatientPortal, PatientPortalRepository>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IInventory, InventoryRepository>();
            services.AddScoped<ISupplier, SupplierRepository>();
            services.AddScoped<ICategory, CategoryRepository>();
            services.AddScoped<INotification, NotificationRepository>();
            services.AddScoped<ICounter, CounterRepository>();
services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<IIPDDashboardRepository, IPDDashboardRepository>();
            services.AddScoped<IPharmacyQueue, PharmacyQueueRepository>();
            services.AddScoped<IPatientHistory, PatientHistoryRepository>();
            services.AddScoped<IAdmissionNotes, AdmissionNoteRepository>();
            services.AddScoped<IDiagnosis, DiagnosisRepository>();
            services.AddScoped<IProcedure, ProcedureRepository>();
            services.AddScoped<IDailyNotes, DailyNotesRepository>();
            services.AddScoped<IInpatientDocument, InpatientDocumentRepository>();
            services.AddScoped<IIPDClinicalExtras, IPDClinicalExtrasRepository>();

            // ── NEW: Treatment Sheet (Clinical Details > Treatment Sheet) ──
            // Purely additive registration - reads/writes its own new
            // tables (ipd_general_order, ipd_general_order_template) plus
            // the new optional columns on ipd_round_prescription /
            // ipd_round_investigation. Does not replace or touch any
            // repository registered above.
            services.AddScoped<ITreatmentSheet, TreatmentSheetRepository>();

            // ── NEW: Extra Orders (Clinical Details > Extra Orders) ──
            // Purely additive registration - own tables (ipd_extra_medication,
            // ipd_extra_order), independent of everything else above.
            services.AddScoped<IExtraOrders, ExtraOrdersRepository>();

            // ── NEW: Lab Reports (Clinical Details > Lab Reports) ──
            // Purely additive registration - own tables (lab_test_category,
            // lab_test_parameter, ipd_lab_report_value), independent of
            // everything else above, including the existing ILabInvestigation
            // (which handles ordered/pending lab investigations, a different
            // concern from this structured parameter-value catalog).
            services.AddScoped<ILabReports, LabReportsRepository>();

            // ── NEW: Radiology Reports (Clinical Details > Radiology Reports) ──
            // Purely additive registration - own tables (ipd_radiology_report,
            // radiology_report_template), independent of everything else above,
            // including the existing ILabReports (a different concern).
            services.AddScoped<IRadiology, RadiologyRepository>();

            // ── NEW: Discharge Planning (Discharge > Discharge Planning) ──
            // Purely additive registration - own table (ipd_discharge_planning),
            // independent of IDischarge (Discharge Summary) and everything else
            // above. Reads billing data live via the existing IIPDBilling
            // rather than duplicating it.
            services.AddScoped<IDischargePlanning, DischargePlanningRepository>();

            // ── NEW: IPD Billing Sheet (IPD tab bar > Billing Sheet) ──
            // Purely additive registration - own table (ipd_billing_sheet),
            // own sp_BS_* stored procedures, independent of everything else
            // above, including the existing IIPDBilling (final consolidated
            // invoice / payments - a different concern from this day-to-day
            // Doctor Billing / Nurse Billing charge log).
            services.AddScoped<IBillingSheet, BillingSheetRepository>();

            // ── NEW: OT Management — Theatre & Booking Scheduling (IPD tab bar > OT Management) ──
            // Purely additive registration - own tables (ot_theatre, ot_booking),
            // own sp_OT_* stored procedures. Consumes the EXISTING IProcedure
            // repository (already registered above) to keep procedure_master in
            // sync after a booking is confirmed — ProcedureController.cs,
            // IProcedure and ProcedureRepository are never modified.
            services.AddScoped<IOTScheduling, OTSchedulingRepository>();

            // ── NEW: IPD Master Billing Ledger (IPD tab bar > Billing Ledger) ──
            // Purely additive registration - widens the EXISTING ipd_bill /
            // ipd_bill_item / ipd_payment tables (see
            // 01_ipd_billing_migration.sql) rather than introducing a
            // parallel ledger. Consumes the EXISTING IBillingMaster (for
            // per-category GST %) and IBillingSheet (Doctor/Nurse Billing)
            // to push their entries into the ledger - neither of those
            // repositories' public contracts are modified.
           
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
else
            {
                // Production: use a clean error page instead of the developer
                // exception page (which recompiles/re-renders stack traces and
                // slows every failed request). Home/Error view already exists.
                app.UseExceptionHandler("/Home/Error");
                // HSTS forces browsers to use HTTPS for the stated period.
                // 30 days is a sensible default (comment out if you don't yet
                // have a valid TLS certificate on your production host).
                app.UseHsts();
            }
            // ── SECURITY: force HTTPS in production ──────────────────────
            // Comment out only if your hosting/load-balancer already handles
            // TLS termination.
            app.UseHttpsRedirection();

            // Compress HTML/JSON/static responses before they hit the wire.
            app.UseResponseCompression();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                   // pattern: "{controller=Login}/{action=PatientLogin}/{id?}");
                   pattern: "{controller=User}/{action=Login}/{id?}");
            });
        }
    }
}