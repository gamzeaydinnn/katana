export interface ErrorState {
  type: "network" | "validation" | "permission" | "unknown";
  message: string;
  retryable: boolean;
}

export const handleStatusUpdateError = (error: any): ErrorState => {
  // Permission error
  if (error.response?.status === 403) {
    return {
      type: "permission",
      message: "Bu işlem için yetkiniz yok",
      retryable: false,
    };
  }

  // Validation error
  if (error.response?.status === 400) {
    return {
      type: "validation",
      message: error.response.data.message || "Geçersiz işlem",
      retryable: false,
    };
  }

  // Network error (no response)
  if (!error.response) {
    return {
      type: "network",
      message: "Bağlantı hatası, lütfen tekrar deneyin",
      retryable: true,
    };
  }

  // Unknown error
  return {
    type: "unknown",
    message: error.response?.data?.message || "Beklenmeyen bir hata oluştu",
    retryable: true,
  };
};
