import React, { useState, useEffect } from "react";
import api from "../../services/api";
import {
  Box,
  Card,
  CardContent,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  TextField,
  MenuItem,
  Button,
  Pagination,
  CircularProgress,
  Alert,
  Tabs,
  Tab,
  Collapse,
  IconButton,
} from "@mui/material";
import {
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Refresh as RefreshIcon,
} from "@mui/icons-material";

interface ErrorLog {
  id: number;
  level: string;
  category: string;
  message: string;
  user: string;
  contextData: string;
  createdAt: string;
}

interface AuditLog {
  id: number;
  actionType: string;
  entityName: string;
  entityId: string;
  performedBy: string;
  details: string;
  ipAddress: string;
  timestamp: string;
}

interface LogStats {
  errorStats: { level: string; count: number }[];
  auditStats: { actionType: string; count: number }[];
  categoryStats: { category: string; count: number }[];
  period: string;
}

const LogsViewer: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);
  const [errorLogs, setErrorLogs] = useState<ErrorLog[]>([]);
  const [auditLogs, setAuditLogs] = useState<AuditLog[]>([]);
  const [stats, setStats] = useState<LogStats | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  // Filters
  const [errorFilters, setErrorFilters] = useState({
    level: "",
    category: "",
    fromDate: "",
    page: 1,
    pageSize: 50,
  });

  const [auditFilters, setAuditFilters] = useState({
    actionType: "",
    entityName: "",
    performedBy: "",
    fromDate: "",
    page: 1,
    pageSize: 50,
  });

  const [totalErrors, setTotalErrors] = useState(0);
  const [totalAudits, setTotalAudits] = useState(0);
  const [expandedRow, setExpandedRow] = useState<number | null>(null);

  // Format date to Turkish timezone (UTC+3)
  const formatToTurkishTime = (dateString: string) => {
    if (!dateString) return "N/A";
    try {
      const date = new Date(dateString);
      // Türkiye saati (UTC+3)
      const turkeyDate = new Date(date.getTime() + 3 * 60 * 60 * 1000);

      const day = String(turkeyDate.getDate()).padStart(2, "0");
      const month = String(turkeyDate.getMonth() + 1).padStart(2, "0");
      const year = turkeyDate.getFullYear();
      const hours = String(turkeyDate.getHours()).padStart(2, "0");
      const minutes = String(turkeyDate.getMinutes()).padStart(2, "0");
      const seconds = String(turkeyDate.getSeconds()).padStart(2, "0");

      return `${day}.${month}.${year} ${hours}:${minutes}:${seconds}`;
    } catch (error) {
      return "Invalid Date";
    }
  };

  useEffect(() => {
    fetchStats();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (tabValue === 0) fetchErrorLogs();
    else if (tabValue === 1) fetchAuditLogs();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tabValue, errorFilters.page, auditFilters.page]);

  const fetchErrorLogs = async () => {
    setLoading(true);
    setError("");
    try {
      const params = new URLSearchParams();
      if (errorFilters.level) params.append("level", errorFilters.level);
      if (errorFilters.category)
        params.append("category", errorFilters.category);
      if (errorFilters.fromDate)
        params.append("fromDate", errorFilters.fromDate);
      params.append("page", errorFilters.page.toString());
      params.append("pageSize", errorFilters.pageSize.toString());

      const { data }: any = await api.get(`/Logs/errors?${params}`);
      setErrorLogs(data.logs);
      setTotalErrors(data.total);
    } catch (err: any) {
      setError(err.response?.data?.error || "Failed to fetch error logs");
    } finally {
      setLoading(false);
    }
  };

  const fetchAuditLogs = async () => {
    setLoading(true);
    setError("");
    try {
      const params = new URLSearchParams();
      if (auditFilters.actionType)
        params.append("actionType", auditFilters.actionType);
      if (auditFilters.entityName)
        params.append("entityName", auditFilters.entityName);
      if (auditFilters.performedBy)
        params.append("performedBy", auditFilters.performedBy);
      if (auditFilters.fromDate)
        params.append("fromDate", auditFilters.fromDate);
      params.append("page", auditFilters.page.toString());
      params.append("pageSize", auditFilters.pageSize.toString());

      const { data }: any = await api.get(`/Logs/audits?${params}`);
      setAuditLogs(data.logs);
      setTotalAudits(data.total);
    } catch (err: any) {
      setError(err.response?.data?.error || "Failed to fetch audit logs");
    } finally {
      setLoading(false);
    }
  };

  const fetchStats = async () => {
    try {
      const { data }: any = await api.get("/Logs/stats");
      setStats(data);
    } catch (err) {
      console.error("Failed to fetch stats", err);
    }
  };

  const getLevelColor = (level: string) => {
    switch (level?.toLowerCase()) {
      case "error":
        return "error";
      case "warning":
        return "warning";
      case "info":
        return "info";
      default:
        return "default";
    }
  };

  const getActionColor = (action: string) => {
    switch (action?.toUpperCase()) {
      case "CREATE":
        return "success";
      case "UPDATE":
        return "info";
      case "DELETE":
        return "error";
      case "LOGIN":
        return "primary";
      default:
        return "default";
    }
  };

  const handleRefresh = () => {
    if (tabValue === 0) fetchErrorLogs();
    else if (tabValue === 1) fetchAuditLogs();
    fetchStats();
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Sistem Logları
      </Typography>

      {/* Stats Cards */}
      {stats && (
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", md: "repeat(3, 1fr)" },
            gap: 2,
            mb: 3,
          }}
        >
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Hata Logları ({stats.period})
              </Typography>
              {stats.errorStats.map((s) => (
                <Chip
                  key={s.level}
                  label={`${s.level}: ${s.count}`}
                  color={getLevelColor(s.level) as any}
                  size="small"
                  sx={{ mr: 1, mb: 1 }}
                />
              ))}
            </CardContent>
          </Card>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Denetim Logları ({stats.period})
              </Typography>
              {stats.auditStats.map((s) => (
                <Chip
                  key={s.actionType}
                  label={`${s.actionType}: ${s.count}`}
                  color={getActionColor(s.actionType) as any}
                  size="small"
                  sx={{ mr: 1, mb: 1 }}
                />
              ))}
            </CardContent>
          </Card>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Kategoriye Göre
              </Typography>
              {stats.categoryStats.slice(0, 5).map((s) => (
                <Chip
                  key={s.category}
                  label={`${s.category}: ${s.count}`}
                  size="small"
                  sx={{ mr: 1, mb: 1 }}
                />
              ))}
            </CardContent>
          </Card>
        </Box>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Card>
        <Box
          sx={{
            borderBottom: 1,
            borderColor: "divider",
            display: "flex",
            justifyContent: "space-between",
            px: 2,
          }}
        >
          <Tabs value={tabValue} onChange={(_, v) => setTabValue(v)}>
            <Tab label={`Hata Logları (${totalErrors})`} />
            <Tab label={`Denetim Logları (${totalAudits})`} />
          </Tabs>
          <Button
            variant="contained"
            startIcon={<RefreshIcon />}
            onClick={handleRefresh}
            sx={{
              my: 1,
              color: "#fff",
              fontWeight: 600,
              background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
              boxShadow: "0 4px 15px rgba(102, 126, 234, 0.4)",
              "&:hover": {
                background: "linear-gradient(135deg, #5568d3 0%, #653a8e 100%)",
                boxShadow: "0 6px 20px rgba(102, 126, 234, 0.6)",
              },
            }}
          >
            Yenile
          </Button>
        </Box>

        <CardContent>
          {/* Error Logs Tab */}
          {tabValue === 0 && (
            <>
              <Box
                sx={{
                  display: "grid",
                  gridTemplateColumns: { xs: "1fr", sm: "repeat(4, 1fr)" },
                  gap: 2,
                  mb: 2,
                }}
              >
                <TextField
                  select
                  fullWidth
                  size="small"
                  label="Seviye"
                  value={errorFilters.level}
                  onChange={(e) =>
                    setErrorFilters({
                      ...errorFilters,
                      level: e.target.value,
                      page: 1,
                    })
                  }
                >
                  <MenuItem value="">Tümü</MenuItem>
                  <MenuItem value="Error">Hata</MenuItem>
                  <MenuItem value="Warning">Uyarı</MenuItem>
                  <MenuItem value="Info">Bilgi</MenuItem>
                </TextField>
                <TextField
                  select
                  fullWidth
                  size="small"
                  label="Category"
                  value={errorFilters.category}
                  onChange={(e) =>
                    setErrorFilters({
                      ...errorFilters,
                      category: e.target.value,
                      page: 1,
                    })
                  }
                >
                  <MenuItem value="">All</MenuItem>
                  <MenuItem value="Authentication">Authentication</MenuItem>
                  <MenuItem value="Sync">Sync</MenuItem>
                  <MenuItem value="ExternalAPI">ExternalAPI</MenuItem>
                  <MenuItem value="UserAction">UserAction</MenuItem>
                  <MenuItem value="System">System</MenuItem>
                  <MenuItem value="Database">Database</MenuItem>
                  <MenuItem value="Business">Business</MenuItem>
                </TextField>
                <TextField
                  type="date"
                  fullWidth
                  size="small"
                  label="Başlangıç Tarihi"
                  value={errorFilters.fromDate}
                  onChange={(e) =>
                    setErrorFilters({
                      ...errorFilters,
                      fromDate: e.target.value,
                      page: 1,
                    })
                  }
                  InputLabelProps={{ shrink: true }}
                />
                <Button
                  fullWidth
                  variant="contained"
                  onClick={fetchErrorLogs}
                  sx={{
                    fontWeight: 600,
                    color: "white",
                    backgroundColor: "#3b82f6",
                    "&:hover": {
                      backgroundColor: "#2563eb",
                    },
                  }}
                >
                  Filter
                </Button>
              </Box>

              {loading ? (
                <CircularProgress />
              ) : (
                <>
                  <TableContainer component={Paper}>
                    <Table>
                      <TableHead>
                        <TableRow>
                          <TableCell width={50}></TableCell>
                          <TableCell>Level</TableCell>
                          <TableCell>Category</TableCell>
                          <TableCell>Message</TableCell>
                          <TableCell>User</TableCell>
                          <TableCell>Date</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {errorLogs.map((log) => (
                          <React.Fragment key={log.id}>
                            <TableRow
                              hover
                              sx={{ cursor: "pointer" }}
                              onClick={() =>
                                setExpandedRow(
                                  expandedRow === log.id ? null : log.id
                                )
                              }
                            >
                              <TableCell>
                                <IconButton size="small">
                                  {expandedRow === log.id ? (
                                    <ExpandLessIcon />
                                  ) : (
                                    <ExpandMoreIcon />
                                  )}
                                </IconButton>
                              </TableCell>
                              <TableCell>
                                <Chip
                                  label={log.level}
                                  color={getLevelColor(log.level) as any}
                                  size="small"
                                />
                              </TableCell>
                              <TableCell>
                                <Chip
                                  label={log.category || "N/A"}
                                  size="small"
                                />
                              </TableCell>
                              <TableCell>{log.message}</TableCell>
                              <TableCell>{log.user || "System"}</TableCell>
                              <TableCell>
                                {formatToTurkishTime(log.createdAt)}
                              </TableCell>
                            </TableRow>
                            <TableRow>
                              <TableCell colSpan={6} sx={{ py: 0 }}>
                                <Collapse
                                  in={expandedRow === log.id}
                                  timeout="auto"
                                  unmountOnExit
                                >
                                  <Box sx={{ p: 2, bgcolor: "grey.50" }}>
                                    {log.contextData && (
                                      <Typography
                                        variant="body2"
                                        sx={{
                                          fontFamily: "monospace",
                                          whiteSpace: "pre-wrap",
                                        }}
                                      >
                                        {log.contextData}
                                      </Typography>
                                    )}
                                  </Box>
                                </Collapse>
                              </TableCell>
                            </TableRow>
                          </React.Fragment>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                  <Box
                    sx={{ display: "flex", justifyContent: "center", mt: 2 }}
                  >
                    <Pagination
                      count={Math.ceil(totalErrors / errorFilters.pageSize)}
                      page={errorFilters.page}
                      onChange={(_, p) =>
                        setErrorFilters({ ...errorFilters, page: p })
                      }
                    />
                  </Box>
                </>
              )}
            </>
          )}

          {/* Audit Logs Tab */}
          {tabValue === 1 && (
            <>
              <Box
                sx={{
                  display: "grid",
                  gridTemplateColumns: { xs: "1fr", sm: "repeat(4, 1fr)" },
                  gap: 2,
                  mb: 2,
                }}
              >
                <TextField
                  select
                  fullWidth
                  size="small"
                  label="İşlem"
                  value={auditFilters.actionType}
                  onChange={(e) =>
                    setAuditFilters({
                      ...auditFilters,
                      actionType: e.target.value,
                      page: 1,
                    })
                  }
                >
                  <MenuItem value="">Tümü</MenuItem>
                  <MenuItem value="CREATE">CREATE</MenuItem>
                  <MenuItem value="UPDATE">UPDATE</MenuItem>
                  <MenuItem value="DELETE">DELETE</MenuItem>
                  <MenuItem value="LOGIN">LOGIN</MenuItem>
                  <MenuItem value="SYNC">SYNC</MenuItem>
                </TextField>
                <TextField
                  fullWidth
                  size="small"
                  label="Varlık Adı"
                  value={auditFilters.entityName}
                  onChange={(e) =>
                    setAuditFilters({
                      ...auditFilters,
                      entityName: e.target.value,
                      page: 1,
                    })
                  }
                />
                <TextField
                  type="date"
                  fullWidth
                  size="small"
                  label="Başlangıç Tarihi"
                  value={auditFilters.fromDate}
                  onChange={(e) =>
                    setAuditFilters({
                      ...auditFilters,
                      fromDate: e.target.value,
                      page: 1,
                    })
                  }
                  InputLabelProps={{ shrink: true }}
                />
                <Button
                  fullWidth
                  variant="contained"
                  onClick={fetchAuditLogs}
                  sx={{
                    fontWeight: 600,
                    color: "white",
                    backgroundColor: "#3b82f6",
                    "&:hover": {
                      backgroundColor: "#2563eb",
                    },
                  }}
                >
                  Uygula
                </Button>
              </Box>

              {loading ? (
                <CircularProgress />
              ) : (
                <>
                  <TableContainer component={Paper}>
                    <Table>
                      <TableHead>
                        <TableRow>
                          <TableCell width={50}></TableCell>
                          <TableCell>İşlem</TableCell>
                          <TableCell>Varlık</TableCell>
                          <TableCell>Varlık ID</TableCell>
                          <TableCell>Kullanıcı</TableCell>
                          <TableCell>IP Adresi</TableCell>
                          <TableCell>Tarih</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {auditLogs.map((log) => (
                          <React.Fragment key={log.id}>
                            <TableRow
                              hover
                              sx={{ cursor: "pointer" }}
                              onClick={() =>
                                setExpandedRow(
                                  expandedRow === log.id ? null : log.id
                                )
                              }
                            >
                              <TableCell>
                                <IconButton size="small">
                                  {expandedRow === log.id ? (
                                    <ExpandLessIcon />
                                  ) : (
                                    <ExpandMoreIcon />
                                  )}
                                </IconButton>
                              </TableCell>
                              <TableCell>
                                <Chip
                                  label={log.actionType}
                                  color={getActionColor(log.actionType) as any}
                                  size="small"
                                />
                              </TableCell>
                              <TableCell>{log.entityName}</TableCell>
                              <TableCell>{log.entityId}</TableCell>
                              <TableCell>{log.performedBy}</TableCell>
                              <TableCell>{log.ipAddress || "N/A"}</TableCell>
                              <TableCell>
                                {formatToTurkishTime(log.timestamp)}
                              </TableCell>
                            </TableRow>
                            <TableRow>
                              <TableCell colSpan={7} sx={{ py: 0 }}>
                                <Collapse
                                  in={expandedRow === log.id}
                                  timeout="auto"
                                  unmountOnExit
                                >
                                  <Box sx={{ p: 2, bgcolor: "grey.50" }}>
                                    <Typography
                                      variant="body2"
                                      sx={{
                                        fontFamily: "monospace",
                                        whiteSpace: "pre-wrap",
                                      }}
                                    >
                                      {log.details || "No details"}
                                    </Typography>
                                  </Box>
                                </Collapse>
                              </TableCell>
                            </TableRow>
                          </React.Fragment>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                  <Box
                    sx={{ display: "flex", justifyContent: "center", mt: 2 }}
                  >
                    <Pagination
                      count={Math.ceil(totalAudits / auditFilters.pageSize)}
                      page={auditFilters.page}
                      onChange={(_, p) =>
                        setAuditFilters({ ...auditFilters, page: p })
                      }
                    />
                  </Box>
                </>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </Box>
  );
};

export default LogsViewer;
