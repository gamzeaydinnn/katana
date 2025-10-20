import axios from "axios";

const API_BASE_URL = "https://akozas.luca.com.tr";

const lucaApi = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
  headers: {
    "Content-Type": "application/json",
  },
});

lucaApi.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error("API Hatası:", error.response?.data || error.message);
    return Promise.reject(error);
  }
);

export default lucaApi;
