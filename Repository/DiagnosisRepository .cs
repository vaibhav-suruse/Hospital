using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class DiagnosisRepository : IDiagnosis
    {
        private readonly string _connectionString;

        public DiagnosisRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        #region Insert Diagnosis (EXISTING — untouched)

        public async Task<int> InsertDiagnosis(DiagnosisModel model)
        {
            int result = 0;

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("SP_InsertDiagnosis", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_IpdId", model.IpdId);
                    cmd.Parameters.AddWithValue("p_PatientId", model.PatientId);
                    cmd.Parameters.AddWithValue("p_DiagnosisName", model.DiagnosisName);
                    cmd.Parameters.AddWithValue("p_DiagnosisType", model.DiagnosisType);
                    cmd.Parameters.AddWithValue("p_RecordedBy", model.RecordedBy);

                    await con.OpenAsync();

                    result = await cmd.ExecuteNonQueryAsync();
                }
            }

            return result;
        }

        #endregion

        #region Get Admission Diagnosis (EXISTING — untouched)

        public async Task<List<DiagnosisModel>> GetAdmissionDiagnosis(int ipdId)
        {
            List<DiagnosisModel> list = new List<DiagnosisModel>();

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("SP_GetAdmissionDiagnosis", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_IpdId", ipdId);

                    await con.OpenAsync();

                    using (MySqlDataReader dr =
                           (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            list.Add(new DiagnosisModel
                            {
                                DiagnosisId = Convert.ToInt32(dr["DiagnosisId"]),
                                IpdId = Convert.ToInt32(dr["IpdId"]),
                                PatientId = Convert.ToInt32(dr["PatientId"]),
                                DiagnosisName = dr["DiagnosisName"].ToString(),
                                DiagnosisType = dr["DiagnosisType"].ToString(),
                                RecordedBy = dr["RecordedBy"].ToString(),
                                DiagnosisDate = Convert.ToDateTime(dr["DiagnosisDate"])
                            });
                        }
                    }
                }
            }

            return list;
        }

        #endregion

        #region Get Discharge Diagnosis (EXISTING — untouched)

        public async Task<List<DiagnosisModel>> GetDischargeDiagnosis(int ipdId)
        {
            List<DiagnosisModel> list = new List<DiagnosisModel>();

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("SP_GetDischargeDiagnosis", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_IpdId", ipdId);

                    await con.OpenAsync();

                    using (MySqlDataReader dr =
                           (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            list.Add(new DiagnosisModel
                            {
                                DiagnosisId = Convert.ToInt32(dr["DiagnosisId"]),
                                IpdId = Convert.ToInt32(dr["IpdId"]),
                                PatientId = Convert.ToInt32(dr["PatientId"]),
                                DiagnosisName = dr["DiagnosisName"].ToString(),
                                DiagnosisType = dr["DiagnosisType"].ToString(),
                                RecordedBy = dr["RecordedBy"].ToString(),
                                DiagnosisDate = Convert.ToDateTime(dr["DiagnosisDate"])
                            });
                        }
                    }
                }
            }

            return list;
        }

        #endregion

        #region Delete Diagnosis (EXISTING — untouched)

        public async Task<int> DeleteDiagnosis(int diagnosisId)
        {
            int result = 0;

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand("SP_DeleteDiagnosis", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_DiagnosisId", diagnosisId);

                    await con.OpenAsync();

                    result = await cmd.ExecuteNonQueryAsync();
                }
            }

            return result;
        }

        #endregion

        // ====================================================================
        // NEW — Phase 1 enterprise methods. All call NEW stored procedures
        // only; nothing above this line was modified.
        // ====================================================================

        #region Insert Diagnosis Enterprise (NEW)

        public async Task<DiagnosisOperationResult> InsertDiagnosisEnterprise(DiagnosisModel model)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_InsertDiagnosisEnterprise", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_IpdId", model.IpdId);
                cmd.Parameters.AddWithValue("p_PatientId", model.PatientId);
                cmd.Parameters.AddWithValue("p_DiagnosisName", model.DiagnosisName);
                cmd.Parameters.AddWithValue("p_DiagnosisType", model.DiagnosisType);
                cmd.Parameters.AddWithValue("p_DiagnosisSubType", (object)model.DiagnosisSubType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_IcdCode", (object)model.IcdCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_DiagnosisMasterId", (object)model.DiagnosisMasterId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_Severity", (object)model.Severity ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_IsPrincipal", model.IsPrincipal);
                cmd.Parameters.AddWithValue("p_PresentOnAdmission", (object)model.PresentOnAdmission ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_HospitalAcquired", model.HospitalAcquired);
                cmd.Parameters.AddWithValue("p_ClinicalNotes", (object)model.ClinicalNotes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_DoctorId", (object)model.DoctorId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_CreatedByUserId", (object)model.CreatedByUserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_HospitalId", (object)model.HospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object)model.SubHospitalId ?? DBNull.Value);

                var outId = new MySqlParameter("p_NewDiagnosisId", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                var outMsg = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outId);
                cmd.Parameters.Add(outMsg);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                int newId = outId.Value == DBNull.Value ? 0 : Convert.ToInt32(outId.Value);

                return new DiagnosisOperationResult
                {
                    Success = newId > 0,
                    Message = outMsg.Value?.ToString(),
                    DiagnosisId = newId
                };
            }
        }

        #endregion

        #region Get Diagnosis By Ipd Enterprise (NEW)

        public async Task<List<DiagnosisModel>> GetDiagnosisByIpdEnterprise(int ipdId, string diagnosisType)
        {
            List<DiagnosisModel> list = new List<DiagnosisModel>();

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_GetDiagnosisByIpdEnterprise", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IpdId", ipdId);
                cmd.Parameters.AddWithValue("p_DiagnosisType", (object)diagnosisType ?? DBNull.Value);

                await con.OpenAsync();

                using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(MapEnterpriseRow(dr));
                    }
                }
            }

            return list;
        }

        private static DiagnosisModel MapEnterpriseRow(MySqlDataReader dr)
        {
            string Str(string col) => dr[col] is DBNull ? null : dr[col].ToString();
            int? IntN(string col) => dr[col] is DBNull ? (int?)null : Convert.ToInt32(dr[col]);
            bool BoolV(string col) => dr[col] is DBNull ? false : Convert.ToBoolean(dr[col]);
            bool? BoolN(string col) => dr[col] is DBNull ? (bool?)null : Convert.ToBoolean(dr[col]);
            DateTime? DateN(string col) => dr[col] is DBNull ? (DateTime?)null : Convert.ToDateTime(dr[col]);

            string enteredByName = $"{Str("RecordedByFirstName")} {Str("RecordedByLastName")}".Trim();
            string verifiedByName = $"{Str("VerifiedByFirstName")} {Str("VerifiedByLastName")}".Trim();
            string approvedByName = $"{Str("ApprovedByFirstName")} {Str("ApprovedByLastName")}".Trim();

            return new DiagnosisModel
            {
                DiagnosisId = Convert.ToInt32(dr["DiagnosisId"]),
                IpdId = Convert.ToInt32(dr["IpdId"]),
                PatientId = Convert.ToInt32(dr["PatientId"]),
                DiagnosisName = Str("DiagnosisName"),
                DiagnosisType = Str("DiagnosisType"),
                RecordedBy = Str("RecordedBy"),
                DiagnosisDate = Convert.ToDateTime(dr["DiagnosisDate"]),
                DiagnosisSubType = Str("DiagnosisSubType"),
                IcdCode = Str("IcdCode"),
                IcdCodeSystem = Str("IcdCodeSystem"),
                DiagnosisMasterId = IntN("DiagnosisMasterId"),
                Severity = Str("Severity"),
                IsPrincipal = BoolV("IsPrincipal"),
                PresentOnAdmission = BoolN("PresentOnAdmission"),
                HospitalAcquired = BoolV("HospitalAcquired"),
                Status = Str("Status") ?? "Active",
                ClinicalNotes = Str("ClinicalNotes"),
                CreatedByUserId = IntN("CreatedByUserId"),
                DoctorId = IntN("DoctorId"),
                EnteredByName = string.IsNullOrWhiteSpace(enteredByName) ? null : enteredByName,
                VerifiedByUserId = IntN("VerifiedByUserId"),
                VerifiedByName = string.IsNullOrWhiteSpace(verifiedByName) ? null : verifiedByName,
                VerifiedDate = DateN("VerifiedDate"),
                ApprovedByUserId = IntN("ApprovedByUserId"),
                ApprovedByName = string.IsNullOrWhiteSpace(approvedByName) ? null : approvedByName,
                ApprovedDate = DateN("ApprovedDate"),
                HospitalId = IntN("HospitalId"),
                SubHospitalId = IntN("SubHospitalId"),
                ModifiedDate = DateN("ModifiedDate"),
                DeletedReason = Str("DeletedReason")
            };
        }

        #endregion

        #region Verify / Approve / Delete Enterprise (NEW)

        public async Task<DiagnosisOperationResult> VerifyDiagnosis(int diagnosisId, int verifiedByUserId, string reason)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_VerifyDiagnosis", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_DiagnosisId", diagnosisId);
                cmd.Parameters.AddWithValue("p_VerifiedByUserId", verifiedByUserId);
                cmd.Parameters.AddWithValue("p_Reason", (object)reason ?? DBNull.Value);

                var outSuccess = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                var outMsg = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outSuccess);
                cmd.Parameters.Add(outMsg);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return new DiagnosisOperationResult
                {
                    Success = outSuccess.Value != DBNull.Value && Convert.ToInt32(outSuccess.Value) == 1,
                    Message = outMsg.Value?.ToString(),
                    DiagnosisId = diagnosisId
                };
            }
        }

        public async Task<DiagnosisOperationResult> ApproveDiagnosis(int diagnosisId, int approvedByUserId, string reason)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_ApproveDiagnosis", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_DiagnosisId", diagnosisId);
                cmd.Parameters.AddWithValue("p_ApprovedByUserId", approvedByUserId);
                cmd.Parameters.AddWithValue("p_Reason", (object)reason ?? DBNull.Value);

                var outSuccess = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                var outMsg = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outSuccess);
                cmd.Parameters.Add(outMsg);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return new DiagnosisOperationResult
                {
                    Success = outSuccess.Value != DBNull.Value && Convert.ToInt32(outSuccess.Value) == 1,
                    Message = outMsg.Value?.ToString(),
                    DiagnosisId = diagnosisId
                };
            }
        }

        public async Task<DiagnosisOperationResult> DeleteDiagnosisEnterprise(int diagnosisId, int deletedByUserId, string reason)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_DeleteDiagnosisEnterprise", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_DiagnosisId", diagnosisId);
                cmd.Parameters.AddWithValue("p_DeletedByUserId", deletedByUserId);
                cmd.Parameters.AddWithValue("p_Reason", (object)reason ?? DBNull.Value);

                var outSuccess = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                var outMsg = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outSuccess);
                cmd.Parameters.Add(outMsg);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return new DiagnosisOperationResult
                {
                    Success = outSuccess.Value != DBNull.Value && Convert.ToInt32(outSuccess.Value) == 1,
                    Message = outMsg.Value?.ToString(),
                    DiagnosisId = diagnosisId
                };
            }
        }

        #endregion

        #region Audit Trail (NEW)

        public async Task<List<DiagnosisAuditModel>> GetDiagnosisAuditTrail(int diagnosisId)
        {
            var list = new List<DiagnosisAuditModel>();

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_GetDiagnosisAuditTrail", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_DiagnosisId", diagnosisId);

                await con.OpenAsync();

                using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        string first = dr["ActionByFirstName"] is DBNull ? "" : dr["ActionByFirstName"].ToString();
                        string last = dr["ActionByLastName"] is DBNull ? "" : dr["ActionByLastName"].ToString();
                        string name = $"{first} {last}".Trim();

                        list.Add(new DiagnosisAuditModel
                        {
                            AuditId = Convert.ToInt32(dr["AuditId"]),
                            DiagnosisId = Convert.ToInt32(dr["DiagnosisId"]),
                            Action = dr["Action"].ToString(),
                            OldValue = dr["OldValue"] is DBNull ? null : dr["OldValue"].ToString(),
                            NewValue = dr["NewValue"] is DBNull ? null : dr["NewValue"].ToString(),
                            Reason = dr["Reason"] is DBNull ? null : dr["Reason"].ToString(),
                            ActionDate = Convert.ToDateTime(dr["ActionDate"]),
                            ActionByUserId = dr["ActionByUserId"] is DBNull ? (int?)null : Convert.ToInt32(dr["ActionByUserId"]),
                            ActionByName = string.IsNullOrWhiteSpace(name) ? null : name,
                            DiagnosisName = dr["DiagnosisName"] is DBNull ? null : dr["DiagnosisName"].ToString(),
                            DiagnosedByName = dr["DiagnosedByName"] is DBNull ? null : dr["DiagnosedByName"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        #endregion

        #region Reports (NEW — Phase 2, read-only)

        public async Task<DiagnosisReportBundle> GetDiagnosisReportBundle(int? hospitalId, int? subHospitalId, DateTime? fromDate, DateTime? toDate)
        {
            var bundle = new DiagnosisReportBundle();

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                await con.OpenAsync();

                object HospitalParam() => (object)hospitalId ?? DBNull.Value;
                object SubHospitalParam() => (object)subHospitalId ?? DBNull.Value;
                object FromParam() => (object)fromDate?.Date ?? DBNull.Value;
                object ToParam() => (object)toDate?.Date ?? DBNull.Value;

                // Doctor-wise
                using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Report_DoctorWise", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_HospitalId", HospitalParam());
                    cmd.Parameters.AddWithValue("p_SubHospitalId", SubHospitalParam());
                    cmd.Parameters.AddWithValue("p_FromDate", FromParam());
                    cmd.Parameters.AddWithValue("p_ToDate", ToParam());

                    using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            bundle.DoctorWise.Add(new DoctorWiseReportRow
                            {
                                DoctorId = dr["DoctorId"] is DBNull ? (int?)null : Convert.ToInt32(dr["DoctorId"]),
                                DoctorName = dr["DoctorName"].ToString(),
                                Specialization = dr["Specialization"] is DBNull ? null : dr["Specialization"].ToString(),
                                DiagnosisCount = Convert.ToInt32(dr["DiagnosisCount"]),
                                PrincipalCount = Convert.ToInt32(dr["PrincipalCount"])
                            });
                        }
                    }
                }

                // ICD distribution
                using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Report_IcdDistribution", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_HospitalId", HospitalParam());
                    cmd.Parameters.AddWithValue("p_SubHospitalId", SubHospitalParam());
                    cmd.Parameters.AddWithValue("p_FromDate", FromParam());
                    cmd.Parameters.AddWithValue("p_ToDate", ToParam());

                    using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            bundle.IcdDistribution.Add(new IcdDistributionRow
                            {
                                IcdCode = dr["IcdCode"].ToString(),
                                DiagnosisName = dr["DiagnosisName"] is DBNull ? null : dr["DiagnosisName"].ToString(),
                                DiagnosisCount = Convert.ToInt32(dr["DiagnosisCount"])
                            });
                        }
                    }
                }

                // Status summary
                using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Report_StatusSummary", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_HospitalId", HospitalParam());
                    cmd.Parameters.AddWithValue("p_SubHospitalId", SubHospitalParam());
                    cmd.Parameters.AddWithValue("p_FromDate", FromParam());
                    cmd.Parameters.AddWithValue("p_ToDate", ToParam());

                    using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            bundle.StatusSummary.Add(new StatusSummaryRow
                            {
                                Status = dr["Status"].ToString(),
                                DiagnosisCount = Convert.ToInt32(dr["DiagnosisCount"])
                            });
                        }
                    }
                }

                // Severity distribution
                using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Report_SeverityDistribution", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_HospitalId", HospitalParam());
                    cmd.Parameters.AddWithValue("p_SubHospitalId", SubHospitalParam());
                    cmd.Parameters.AddWithValue("p_FromDate", FromParam());
                    cmd.Parameters.AddWithValue("p_ToDate", ToParam());

                    using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            bundle.SeverityDistribution.Add(new SeverityDistributionRow
                            {
                                Severity = dr["Severity"].ToString(),
                                DiagnosisCount = Convert.ToInt32(dr["DiagnosisCount"])
                            });
                        }
                    }
                }

                // Sub-type distribution
                using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Report_SubTypeDistribution", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_HospitalId", HospitalParam());
                    cmd.Parameters.AddWithValue("p_SubHospitalId", SubHospitalParam());
                    cmd.Parameters.AddWithValue("p_FromDate", FromParam());
                    cmd.Parameters.AddWithValue("p_ToDate", ToParam());

                    using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            bundle.SubTypeDistribution.Add(new SubTypeDistributionRow
                            {
                                DiagnosisSubType = dr["DiagnosisSubType"].ToString(),
                                DiagnosisCount = Convert.ToInt32(dr["DiagnosisCount"])
                            });
                        }
                    }
                }

                // Hospital-Acquired Conditions
                using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Report_HAC", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_HospitalId", HospitalParam());
                    cmd.Parameters.AddWithValue("p_SubHospitalId", SubHospitalParam());
                    cmd.Parameters.AddWithValue("p_FromDate", FromParam());
                    cmd.Parameters.AddWithValue("p_ToDate", ToParam());

                    using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            bundle.HospitalAcquiredConditions.Add(new HacReportRow
                            {
                                DiagnosisId = Convert.ToInt32(dr["DiagnosisId"]),
                                IpdId = Convert.ToInt32(dr["IpdId"]),
                                PatientId = Convert.ToInt32(dr["PatientId"]),
                                DiagnosisName = dr["DiagnosisName"] is DBNull ? null : dr["DiagnosisName"].ToString(),
                                IcdCode = dr["IcdCode"] is DBNull ? null : dr["IcdCode"].ToString(),
                                Severity = dr["Severity"] is DBNull ? null : dr["Severity"].ToString(),
                                DoctorName = dr["DoctorName"] is DBNull ? null : dr["DoctorName"].ToString(),
                                DiagnosisDate = Convert.ToDateTime(dr["DiagnosisDate"]),
                                Status = dr["Status"].ToString()
                            });
                        }
                    }
                }
            }

            return bundle;
        }

        #endregion

        #region Diagnosis Type Master (NEW — Phase 3, read-only)

        public async Task<List<DiagnosisTypeMaster>> GetDiagnosisTypes()
        {
            var list = new List<DiagnosisTypeMaster>();

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_DiagnosisType_GetAll", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                await con.OpenAsync();

                using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new DiagnosisTypeMaster
                        {
                            Id = Convert.ToInt32(dr["Id"]),
                            TypeName = dr["TypeName"].ToString(),
                            Description = dr["Description"] is DBNull ? null : dr["Description"].ToString(),
                            RequiresIcdCode = Convert.ToBoolean(dr["RequiresIcdCode"]),
                            RequiresApprovalBeforeDischarge = Convert.ToBoolean(dr["RequiresApprovalBeforeDischarge"]),
                            SortOrder = Convert.ToInt32(dr["SortOrder"])
                        });
                    }
                }
            }

            return list;
        }

        #endregion

        // ====================================================================
        // NEW — Tier 1-7 hardening pass methods. All call BRAND-NEW stored
        // procedures (SP_...V2 / SP_Resolve.../ SP_Reject.../ SP_Freeze.../
        // SP_Unlock.../ SP_Diagnosis_.../ SP_AddDiagnosisEvidence, etc. — see
        // 19_diagnosis_enterprise_routines.sql). Nothing above this line was
        // touched, and no existing stored procedure was modified.
        // ====================================================================

        #region Role Permission Matrix (Tier 3 — diagnosis-scoped only)

        // Plain parameterised query against a simple lookup table rather than
        // a stored procedure — this table is meant to be editable by Admin
        // directly (or via a future small admin screen) without needing a
        // deployment, so keeping the read trivial here is intentional.
        public async Task<HashSet<string>> GetAllowedActionsForRole(string role)
        {
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(role)) return allowed;

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand(
                "SELECT ActionName FROM diagnosis_role_permission WHERE Role = @role AND IsAllowed = 1", con))
            {
                cmd.Parameters.AddWithValue("@role", role);
                await con.OpenAsync();

                using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        allowed.Add(dr["ActionName"].ToString());
                    }
                }
            }

            return allowed;
        }

        #endregion

        #region Insert Diagnosis Enterprise V2 (Tier 1/2/4)

        public async Task<DiagnosisOperationResult> InsertDiagnosisEnterpriseV2(DiagnosisModel model)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_InsertDiagnosisEnterpriseV2", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_IpdId", model.IpdId);
                cmd.Parameters.AddWithValue("p_PatientId", model.PatientId);
                cmd.Parameters.AddWithValue("p_DiagnosisName", model.DiagnosisName);
                cmd.Parameters.AddWithValue("p_DiagnosisType", model.DiagnosisType);
                cmd.Parameters.AddWithValue("p_DiagnosisSubType", (object)model.DiagnosisSubType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_IcdCode", (object)model.IcdCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_Icd11Code", (object)model.Icd11Code ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_SnomedCode", (object)model.SnomedCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_DiagnosisMasterId", (object)model.DiagnosisMasterId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_Severity", (object)model.Severity ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_Stage", (object)model.Stage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_Grade", (object)model.Grade ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_Laterality", (object)model.Laterality ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_BodySite", (object)model.BodySite ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_IsPrincipal", model.IsPrincipal);
                cmd.Parameters.AddWithValue("p_PresentOnAdmission", (object)model.PresentOnAdmission ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_HospitalAcquired", model.HospitalAcquired);
                cmd.Parameters.AddWithValue("p_ClinicalNotes", (object)model.ClinicalNotes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_Prognosis", (object)model.Prognosis ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_TreatmentPlan", (object)model.TreatmentPlan ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_ExpectedOutcome", (object)model.ExpectedOutcome ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_InsuranceRelevant", model.InsuranceRelevant);
                cmd.Parameters.AddWithValue("p_DoctorId", (object)model.DoctorId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_CreatedByUserId", (object)model.CreatedByUserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_HospitalId", (object)model.HospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object)model.SubHospitalId ?? DBNull.Value);
                object diagDateParam = model.DiagnosisDate == default(DateTime) ? (object)DBNull.Value : model.DiagnosisDate;
                cmd.Parameters.AddWithValue("p_DiagnosisDate", diagDateParam);

                var outId = new MySqlParameter("p_NewDiagnosisId", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                var outMsg = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outId);
                cmd.Parameters.Add(outMsg);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                int newId = outId.Value == DBNull.Value ? 0 : Convert.ToInt32(outId.Value);

                return new DiagnosisOperationResult
                {
                    Success = newId > 0,
                    Message = outMsg.Value?.ToString(),
                    DiagnosisId = newId
                };
            }
        }

        #endregion

        #region Resolve / Reject / Freeze / Unlock (Tier 2/3)

        public async Task<DiagnosisOperationResult> ResolveDiagnosis(int diagnosisId, DateTime resolvedDate, int userId, string reason)
        {
            return await RunSuccessMessageProc("SP_ResolveDiagnosis", diagnosisId, cmd =>
            {
                cmd.Parameters.AddWithValue("p_ResolvedDate", resolvedDate);
                cmd.Parameters.AddWithValue("p_UserId", userId);
                cmd.Parameters.AddWithValue("p_Reason", (object)reason ?? DBNull.Value);
            });
        }

        public async Task<DiagnosisOperationResult> RejectDiagnosis(int diagnosisId, int userId, string reason)
        {
            return await RunSuccessMessageProc("SP_RejectDiagnosis", diagnosisId, cmd =>
            {
                cmd.Parameters.AddWithValue("p_UserId", userId);
                cmd.Parameters.AddWithValue("p_Reason", (object)reason ?? DBNull.Value);
            });
        }

        public async Task<DiagnosisOperationResult> FreezeDiagnosis(int diagnosisId, int userId)
        {
            return await RunSuccessMessageProc("SP_FreezeDiagnosis", diagnosisId, cmd =>
            {
                cmd.Parameters.AddWithValue("p_UserId", userId);
            });
        }

        public async Task<DiagnosisOperationResult> UnlockDiagnosis(int diagnosisId, int userId)
        {
            return await RunSuccessMessageProc("SP_UnlockDiagnosis", diagnosisId, cmd =>
            {
                cmd.Parameters.AddWithValue("p_UserId", userId);
            });
        }

        // Shared helper for the common "p_DiagnosisId in, p_Success/p_Message out"
        // shape used by Resolve/Reject/Freeze/Unlock, so each action doesn't
        // repeat the same connection/output-param plumbing.
        private async Task<DiagnosisOperationResult> RunSuccessMessageProc(string procName, int diagnosisId, Action<MySqlCommand> addParams)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand(procName, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_DiagnosisId", diagnosisId);
                addParams(cmd);

                var outSuccess = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                var outMsg = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outSuccess);
                cmd.Parameters.Add(outMsg);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return new DiagnosisOperationResult
                {
                    Success = outSuccess.Value != DBNull.Value && Convert.ToInt32(outSuccess.Value) == 1,
                    Message = outMsg.Value?.ToString(),
                    DiagnosisId = diagnosisId
                };
            }
        }

        #endregion

        #region Linked Clinical Evidence (Tier 4)

        public async Task<DiagnosisOperationResult> AddDiagnosisEvidence(DiagnosisEvidenceRequest request, int userId)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_AddDiagnosisEvidence", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_DiagnosisId", request.DiagnosisId);
                cmd.Parameters.AddWithValue("p_EvidenceType", request.EvidenceType);
                cmd.Parameters.AddWithValue("p_ReferenceId", (object)request.ReferenceId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_ReferenceLabel", request.ReferenceLabel);
                cmd.Parameters.AddWithValue("p_Notes", (object)request.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_UserId", userId);

                var outSuccess = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                var outMsg = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outSuccess);
                cmd.Parameters.Add(outMsg);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return new DiagnosisOperationResult
                {
                    Success = outSuccess.Value != DBNull.Value && Convert.ToInt32(outSuccess.Value) == 1,
                    Message = outMsg.Value?.ToString(),
                    DiagnosisId = request.DiagnosisId
                };
            }
        }

        public async Task<List<DiagnosisEvidenceModel>> GetDiagnosisEvidence(int diagnosisId)
        {
            var list = new List<DiagnosisEvidenceModel>();

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_GetDiagnosisEvidence", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_DiagnosisId", diagnosisId);

                await con.OpenAsync();

                using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new DiagnosisEvidenceModel
                        {
                            EvidenceId = Convert.ToInt32(dr["EvidenceId"]),
                            DiagnosisId = Convert.ToInt32(dr["DiagnosisId"]),
                            EvidenceType = dr["EvidenceType"].ToString(),
                            ReferenceId = dr["ReferenceId"] is DBNull ? (int?)null : Convert.ToInt32(dr["ReferenceId"]),
                            ReferenceLabel = dr["ReferenceLabel"].ToString(),
                            Notes = dr["Notes"] is DBNull ? null : dr["Notes"].ToString(),
                            CreatedDate = Convert.ToDateTime(dr["CreatedDate"])
                        });
                    }
                }
            }

            return list;
        }

        #endregion

        #region Missing-ICD Clinical Alert (Tier 2)

        // Plain parameterised query (read-only, single small screen-load
        // check) — surfaces the same "ICD missing before discharge" rule the
        // enterprise insert SP already enforces at save time, but as a
        // proactive alert instead of only a save-time rejection.
        public async Task<List<DiagnosisModel>> GetMissingIcdAlerts(int ipdId)
        {
            var list = new List<DiagnosisModel>();

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand(
                @"SELECT DiagnosisId, IpdId, PatientId, DiagnosisName, DiagnosisType, DiagnosisSubType, Status, DiagnosisDate
                  FROM ipddiagnosis
                  WHERE IpdId = @ipdId
                    AND DiagnosisType = 'Discharge'
                    AND IsActive = 1
                    AND Status NOT IN ('RuledOut','Rejected')
                    AND (IcdCode IS NULL OR TRIM(IcdCode) = '')", con))
            {
                cmd.Parameters.AddWithValue("@ipdId", ipdId);
                await con.OpenAsync();

                using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new DiagnosisModel
                        {
                            DiagnosisId = Convert.ToInt32(dr["DiagnosisId"]),
                            IpdId = Convert.ToInt32(dr["IpdId"]),
                            PatientId = Convert.ToInt32(dr["PatientId"]),
                            DiagnosisName = dr["DiagnosisName"].ToString(),
                            DiagnosisType = dr["DiagnosisType"].ToString(),
                            DiagnosisSubType = dr["DiagnosisSubType"] is DBNull ? null : dr["DiagnosisSubType"].ToString(),
                            Status = dr["Status"].ToString(),
                            DiagnosisDate = Convert.ToDateTime(dr["DiagnosisDate"])
                        });
                    }
                }
            }

            return list;
        }

        #endregion

        #region Tier 5 Reports — Mortality / Morbidity / Readmission / Notifiable

        public async Task<List<MortalityReportRow>> GetMortalityReport(int? hospitalId, int? subHospitalId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<MortalityReportRow>();
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Report_Mortality", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", (object)hospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object)subHospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_FromDate", (object)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_ToDate", (object)toDate ?? DBNull.Value);

                await con.OpenAsync();
                using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new MortalityReportRow
                        {
                            DiagnosisId = Convert.ToInt32(dr["DiagnosisId"]),
                            IpdId = Convert.ToInt32(dr["IpdId"]),
                            PatientId = Convert.ToInt32(dr["PatientId"]),
                            DiagnosisName = dr["DiagnosisName"] is DBNull ? null : dr["DiagnosisName"].ToString(),
                            IcdCode = dr["IcdCode"] is DBNull ? null : dr["IcdCode"].ToString(),
                            DiagnosisSubType = dr["DiagnosisSubType"] is DBNull ? null : dr["DiagnosisSubType"].ToString(),
                            DiagnosisDate = Convert.ToDateTime(dr["DiagnosisDate"]),
                            DoctorName = dr["DoctorName"] is DBNull ? null : dr["DoctorName"].ToString(),
                            Status = dr["Status"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public async Task<List<MorbidityReportRow>> GetMorbidityReport(int? hospitalId, int? subHospitalId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<MorbidityReportRow>();
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Report_Morbidity", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", (object)hospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object)subHospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_FromDate", (object)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_ToDate", (object)toDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_TopN", 100);

                await con.OpenAsync();
                using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new MorbidityReportRow
                        {
                            DiagnosisName = dr["DiagnosisName"] is DBNull ? null : dr["DiagnosisName"].ToString(),
                            IcdCode = dr["IcdCode"] is DBNull ? null : dr["IcdCode"].ToString(),
                            CaseCount = Convert.ToInt32(dr["CaseCount"]),
                            PrincipalCount = Convert.ToInt32(dr["PrincipalCount"])
                        });
                    }
                }
            }
            return list;
        }

        public async Task<List<ReadmissionReportRow>> GetReadmissionReport(int? hospitalId, int? subHospitalId, DateTime? fromDate, DateTime? toDate, int windowDays)
        {
            var list = new List<ReadmissionReportRow>();
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Report_Readmission", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", (object)hospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object)subHospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_FromDate", (object)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_ToDate", (object)toDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_WindowDays", windowDays <= 0 ? 30 : windowDays);

                await con.OpenAsync();
                using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new ReadmissionReportRow
                        {
                            PatientId = Convert.ToInt32(dr["PatientId"]),
                            PreviousIpdId = Convert.ToInt32(dr["PreviousIpdId"]),
                            PreviousDischargeDate = dr["PreviousDischargeDate"] is DBNull ? (DateTime?)null : Convert.ToDateTime(dr["PreviousDischargeDate"]),
                            ReadmissionIpdId = Convert.ToInt32(dr["ReadmissionIpdId"]),
                            ReadmissionDate = Convert.ToDateTime(dr["ReadmissionDate"]),
                            DaysBetween = Convert.ToInt32(dr["DaysBetween"]),
                            PreviousDiagnosis = dr["PreviousDiagnosis"] is DBNull ? null : dr["PreviousDiagnosis"].ToString(),
                            ReadmissionDiagnosis = dr["ReadmissionDiagnosis"] is DBNull ? null : dr["ReadmissionDiagnosis"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public async Task<List<NotifiableDiseaseReportRow>> GetNotifiableDiseaseReport(int? hospitalId, int? subHospitalId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<NotifiableDiseaseReportRow>();
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Report_Notifiable", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", (object)hospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object)subHospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_FromDate", (object)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_ToDate", (object)toDate ?? DBNull.Value);

                await con.OpenAsync();
                using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new NotifiableDiseaseReportRow
                        {
                            DiagnosisId = Convert.ToInt32(dr["DiagnosisId"]),
                            IpdId = Convert.ToInt32(dr["IpdId"]),
                            PatientId = Convert.ToInt32(dr["PatientId"]),
                            DiagnosisName = dr["DiagnosisName"] is DBNull ? null : dr["DiagnosisName"].ToString(),
                            IcdCode = dr["IcdCode"] is DBNull ? null : dr["IcdCode"].ToString(),
                            DiagnosisDate = Convert.ToDateTime(dr["DiagnosisDate"]),
                            DoctorName = dr["DoctorName"] is DBNull ? null : dr["DoctorName"].ToString(),
                            Status = dr["Status"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        #endregion

        #region Tier 6 — Pagination/Filters, Favourites, Bulk Actions

        public async Task<PagedDiagnosisResult> GetDiagnosisPaged(DiagnosisFilterRequest filter)
        {
            var result = new PagedDiagnosisResult { PageNumber = filter.PageNumber, PageSize = filter.PageSize };

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_GetPaged", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IpdId", filter.IpdId);
                cmd.Parameters.AddWithValue("p_DiagnosisType", (object)filter.DiagnosisType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_Status", (object)filter.Status ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_Severity", (object)filter.Severity ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_DiagnosisSubType", (object)filter.DiagnosisSubType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_SearchTerm", (object)filter.SearchTerm ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_PageNumber", filter.PageNumber);
                cmd.Parameters.AddWithValue("p_PageSize", filter.PageSize);

                await con.OpenAsync();

                using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    // First result set: TotalCount
                    if (await dr.ReadAsync())
                    {
                        result.TotalCount = Convert.ToInt32(dr["TotalCount"]);
                    }

                    // Second result set: the page of rows (base ipddiagnosis columns only)
                    if (await dr.NextResultAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            result.Items.Add(MapBaseRow(dr));
                        }
                    }
                }
            }

            return result;
        }

        // Maps a plain `SELECT * FROM ipddiagnosis` row (used by the paged
        // grid) — narrower than MapEnterpriseRow, which expects the extra
        // joined name columns SP_GetDiagnosisByIpdEnterprise adds.
        private static DiagnosisModel MapBaseRow(MySqlDataReader dr)
        {
            string Str(string col) => dr[col] is DBNull ? null : dr[col].ToString();
            int? IntN(string col) => dr[col] is DBNull ? (int?)null : Convert.ToInt32(dr[col]);
            bool BoolV(string col) => dr[col] is DBNull ? false : Convert.ToBoolean(dr[col]);
            bool? BoolN(string col) => dr[col] is DBNull ? (bool?)null : Convert.ToBoolean(dr[col]);
            DateTime? DateN(string col) => dr[col] is DBNull ? (DateTime?)null : Convert.ToDateTime(dr[col]);

            return new DiagnosisModel
            {
                DiagnosisId = Convert.ToInt32(dr["DiagnosisId"]),
                IpdId = Convert.ToInt32(dr["IpdId"]),
                PatientId = Convert.ToInt32(dr["PatientId"]),
                DiagnosisName = Str("DiagnosisName"),
                DiagnosisType = Str("DiagnosisType"),
                RecordedBy = Str("RecordedBy"),
                DiagnosisDate = Convert.ToDateTime(dr["DiagnosisDate"]),
                DiagnosisSubType = Str("DiagnosisSubType"),
                IcdCode = Str("IcdCode"),
                IcdCodeSystem = Str("IcdCodeSystem"),
                Icd11Code = Str("Icd11Code"),
                SnomedCode = Str("SnomedCode"),
                DiagnosisMasterId = IntN("DiagnosisMasterId"),
                Severity = Str("Severity"),
                Stage = Str("Stage"),
                Grade = Str("Grade"),
                Laterality = Str("Laterality"),
                BodySite = Str("BodySite"),
                IsPrincipal = BoolV("IsPrincipal"),
                PresentOnAdmission = BoolN("PresentOnAdmission"),
                HospitalAcquired = BoolV("HospitalAcquired"),
                Status = Str("Status") ?? "Active",
                ResolvedDate = DateN("ResolvedDate"),
                ClinicalNotes = Str("ClinicalNotes"),
                Prognosis = Str("Prognosis"),
                TreatmentPlan = Str("TreatmentPlan"),
                ExpectedOutcome = Str("ExpectedOutcome"),
                CreatedByUserId = IntN("CreatedByUserId"),
                DoctorId = IntN("DoctorId"),
                VerifiedByUserId = IntN("VerifiedByUserId"),
                VerifiedDate = DateN("VerifiedDate"),
                ApprovedByUserId = IntN("ApprovedByUserId"),
                ApprovedDate = DateN("ApprovedDate"),
                HospitalId = IntN("HospitalId"),
                SubHospitalId = IntN("SubHospitalId"),
                ModifiedDate = DateN("ModifiedDate"),
                DeletedReason = Str("DeletedReason"),
                IsFrozen = BoolV("IsFrozen"),
                FrozenByUserId = IntN("FrozenByUserId"),
                FrozenDate = DateN("FrozenDate"),
                InsuranceRelevant = BoolV("InsuranceRelevant"),
                CodingCompleted = BoolV("CodingCompleted"),
                ClaimSubmitted = BoolV("ClaimSubmitted")
            };
        }

        public async Task<DiagnosisOperationResult> AddFavorite(int userId, int? hospitalId, AddFavoriteRequest request)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Favorite_Add", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_UserId", userId);
                cmd.Parameters.AddWithValue("p_HospitalId", (object)hospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_DiagnosisName", request.DiagnosisName);
                cmd.Parameters.AddWithValue("p_IcdCode", (object)request.IcdCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_DiagnosisMasterId", (object)request.DiagnosisMasterId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_DiagnosisSubType", (object)request.DiagnosisSubType ?? DBNull.Value);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return new DiagnosisOperationResult { Success = true, Message = "Added to favourites." };
            }
        }

        public async Task<List<DiagnosisFavorite>> GetFavorites(int userId)
        {
            var list = new List<DiagnosisFavorite>();
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Favorite_GetByUser", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_UserId", userId);

                await con.OpenAsync();
                using (MySqlDataReader dr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new DiagnosisFavorite
                        {
                            Id = Convert.ToInt32(dr["Id"]),
                            UserId = Convert.ToInt32(dr["UserId"]),
                            DiagnosisName = dr["DiagnosisName"].ToString(),
                            IcdCode = dr["IcdCode"] is DBNull ? null : dr["IcdCode"].ToString(),
                            DiagnosisMasterId = dr["DiagnosisMasterId"] is DBNull ? (int?)null : Convert.ToInt32(dr["DiagnosisMasterId"]),
                            DiagnosisSubType = dr["DiagnosisSubType"] is DBNull ? null : dr["DiagnosisSubType"].ToString(),
                            UsageCount = Convert.ToInt32(dr["UsageCount"])
                        });
                    }
                }
            }
            return list;
        }

        public async Task<DiagnosisOperationResult> RemoveFavorite(int id, int userId)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_Favorite_Remove", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", id);
                cmd.Parameters.AddWithValue("p_UserId", userId);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return new DiagnosisOperationResult { Success = true, Message = "Removed from favourites." };
            }
        }

        public async Task<DiagnosisOperationResult> BulkVerify(List<int> diagnosisIds, int userId, string reason)
        {
            return await RunBulkProc("SP_Diagnosis_BulkVerify", diagnosisIds, userId, reason);
        }

        public async Task<DiagnosisOperationResult> BulkApprove(List<int> diagnosisIds, int userId, string reason)
        {
            return await RunBulkProc("SP_Diagnosis_BulkApprove", diagnosisIds, userId, reason);
        }

        private async Task<DiagnosisOperationResult> RunBulkProc(string procName, List<int> diagnosisIds, int userId, string reason)
        {
            if (diagnosisIds == null || diagnosisIds.Count == 0)
            {
                return new DiagnosisOperationResult { Success = false, Message = "No diagnoses selected." };
            }

            string json = "[" + string.Join(",", diagnosisIds) + "]";

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand(procName, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_DiagnosisIdsJson", json);
                cmd.Parameters.AddWithValue("p_UserId", userId);
                cmd.Parameters.AddWithValue("p_Reason", (object)reason ?? DBNull.Value);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return new DiagnosisOperationResult { Success = true, Message = $"{diagnosisIds.Count} diagnosis record(s) updated." };
            }
        }

        #endregion

        #region Billing / Insurance Coding Flags (Tier 7)

        public async Task<DiagnosisOperationResult> UpdateBillingFlags(DiagnosisBillingFlagsRequest request, int userId)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_Diagnosis_UpdateBillingFlags", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_DiagnosisId", request.DiagnosisId);
                cmd.Parameters.AddWithValue("p_InsuranceRelevant", request.InsuranceRelevant);
                cmd.Parameters.AddWithValue("p_CodingCompleted", request.CodingCompleted);
                cmd.Parameters.AddWithValue("p_ClaimSubmitted", request.ClaimSubmitted);
                cmd.Parameters.AddWithValue("p_UserId", userId);

                var outSuccess = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                var outMsg = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outSuccess);
                cmd.Parameters.Add(outMsg);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return new DiagnosisOperationResult
                {
                    Success = outSuccess.Value != DBNull.Value && Convert.ToInt32(outSuccess.Value) == 1,
                    Message = outMsg.Value?.ToString(),
                    DiagnosisId = request.DiagnosisId
                };
            }
        }

        #endregion

        #region Multi-hospital data isolation helper (NEW)

        public async Task<int?> GetDiagnosisOwnerIpdIdAsync(int diagnosisId)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("SELECT IpdId FROM ipddiagnosis WHERE DiagnosisId = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", diagnosisId);
                await con.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
            }
        }

        #endregion
    }
}
