import React, { useState, useEffect } from "react";
import axios from "axios";
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

      const { data } = await axios.get(`/api/Logs/errors?${params}`);
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

      const { data } = await axios.get(`/api/Logs/audits?${params}`);
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
      const { data } = await axios.get("/api/Logs/stats");
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
        System Logs
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
                Error Logs ({stats.period})
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
                Audit Logs ({stats.period})
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
                By Category
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
            <Tab label={`Error Logs (${totalErrors})`} />
            <Tab label={`Audit Logs (${totalAudits})`} />
          </Tabs>
          <Button
            startIcon={<RefreshIcon />}
            onClick={handleRefresh}
            sx={{ my: 1 }}
          >
            Refresh
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
                  label="Level"
                  value={errorFilters.level}
                  onChange={(e) =>
                    setErrorFilters({
                      ...errorFilters,
                      level: e.target.value,
                      page: 1,
                    })
                  }
                >
                  <MenuItem value="">All</MenuItem>
                  <MenuItem value="Error">Error</MenuItem>
                  <MenuItem value="Warning">Warning</MenuItem>
                  <MenuItem value="Info">Info</MenuItem>
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
                  label="From Date"
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
                <Button fullWidth variant="contained" onClick={fetchErrorLogs}>
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
                                {new Date(log.createdAt).toLocaleString()}
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
                  label="Action"
                  value={auditFilters.actionType}
                  onChange={(e) =>
                    setAuditFilters({
                      ...auditFilters,
                      actionType: e.target.value,
                      page: 1,
                    })
                  }
                >
                  <MenuItem value="">All</MenuItem>
                  <MenuItem value="CREATE">CREATE</MenuItem>
                  <MenuItem value="UPDATE">UPDATE</MenuItem>
                  <MenuItem value="DELETE">DELETE</MenuItem>
                  <MenuItem value="LOGIN">LOGIN</MenuItem>
                  <MenuItem value="SYNC">SYNC</MenuItem>
                </TextField>
                <TextField
                  fullWidth
                  size="small"
                  label="Entity Name"
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
                  label="From Date"
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
                <Button fullWidth variant="contained" onClick={fetchAuditLogs}>
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
                          <TableCell>Action</TableCell>
                          <TableCell>Entity</TableCell>
                          <TableCell>Entity ID</TableCell>
                          <TableCell>User</TableCell>
                          <TableCell>IP Address</TableCell>
                          <TableCell>Date</TableCell>
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
                                {new Date(log.timestamp).toLocaleString()}
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
