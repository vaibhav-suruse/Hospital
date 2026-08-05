using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class InpatientDocumentRepository : IInpatientDocument
    {

        private readonly string _connectionString;

        public InpatientDocumentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        public List<InpatientDocumentModel> GetDocuments(int ipId)
        {
            List<InpatientDocumentModel> list = new List<InpatientDocumentModel>();

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                MySqlCommand cmd = new MySqlCommand("sp_InpatientDocument_GetAll", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_IP_ID", ipId);

                con.Open();

                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new InpatientDocumentModel
                        {
                            DocumentId = Convert.ToInt32(dr["DocumentId"]),
                            IP_ID = Convert.ToInt32(dr["IP_ID"]),
                            FileName = dr["FileName"].ToString(),
                            FilePath = dr["FilePath"].ToString(),
                            FileType = dr["FileType"].ToString(),
                            FileSize = Convert.ToInt64(dr["FileSize"]),
                            UploadedDate = Convert.ToDateTime(dr["UploadedDate"])
                        });
                    }
                }
            }

            return list;
        }

        public InpatientDocumentModel? GetDocumentById(int documentId)
        {
            InpatientDocumentModel model = null;

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                MySqlCommand cmd = new MySqlCommand("sp_InpatientDocument_GetById", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_DocumentId", documentId);

                con.Open();

                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        model = new InpatientDocumentModel
                        {
                            DocumentId = Convert.ToInt32(dr["DocumentId"]),
                            IP_ID = Convert.ToInt32(dr["IP_ID"]),
                            FileName = dr["FileName"].ToString(),
                            FilePath = dr["FilePath"].ToString(),
                            FileType = dr["FileType"].ToString(),
                            FileSize = Convert.ToInt64(dr["FileSize"]),
                            UploadedBy = Convert.ToInt32(dr["UploadedBy"]),
                            UploadedDate = Convert.ToDateTime(dr["UploadedDate"]),
                            IsDeleted = Convert.ToBoolean(dr["IsDeleted"])
                        };
                    }
                }
            }

            return model;
        }

        public int InsertDocument(InpatientDocumentModel model)
        {
            int documentId = 0;

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                MySqlCommand cmd = new MySqlCommand("sp_InpatientDocument_Insert", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_IP_ID", model.IP_ID);
                cmd.Parameters.AddWithValue("p_FileName", model.FileName);
                cmd.Parameters.AddWithValue("p_FilePath", model.FilePath);
                cmd.Parameters.AddWithValue("p_FileType", model.FileType);
                cmd.Parameters.AddWithValue("p_FileSize", model.FileSize);
                cmd.Parameters.AddWithValue("p_UploadedBy", model.UploadedBy);

                con.Open();

                documentId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            return documentId;
        }

        public void DeleteDocument(int documentId)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                MySqlCommand cmd = new MySqlCommand("sp_InpatientDocument_Delete", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_DocumentId", documentId);

                con.Open();

                cmd.ExecuteNonQuery();
            }
        }
    }
}
