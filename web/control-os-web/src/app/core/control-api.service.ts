import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, isDevMode } from '@angular/core';
import { Observable } from 'rxjs';
import {
  BackupResponse,
  DashboardResponse,
  Device,
  DevicePowerOperation,
  DeviceUpsertRequest,
  GlobalSettings,
  LogsResponse,
  NetworkScanRequest,
  NetworkScanResult,
  OperationResponse
} from './api.models';

@Injectable({ providedIn: 'root' })
export class ControlApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = isDevMode() ? 'http://localhost:5081/api' : '/api';

  getDashboard(refresh = true, logLines = 20): Observable<DashboardResponse> {
    const params = new HttpParams()
      .set('refresh', refresh)
      .set('logLines', logLines);

    return this.http.get<DashboardResponse>(`${this.apiBaseUrl}/dashboard`, { params });
  }

  getDevices(): Observable<Device[]> {
    return this.http.get<Device[]>(`${this.apiBaseUrl}/devices`);
  }

  createDevice(request: DeviceUpsertRequest): Observable<Device> {
    return this.http.post<Device>(`${this.apiBaseUrl}/devices`, request);
  }

  updateDevice(deviceId: string, request: DeviceUpsertRequest): Observable<Device> {
    return this.http.put<Device>(`${this.apiBaseUrl}/devices/${deviceId}`, request);
  }

  deleteDevice(deviceId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/devices/${deviceId}`);
  }

  executeOperation(deviceId: string, operation: DevicePowerOperation): Observable<OperationResponse> {
    const endpoint = operation.toLowerCase();
    return this.http.post<OperationResponse>(`${this.apiBaseUrl}/devices/${deviceId}/${endpoint}`, {});
  }

  getSettings(): Observable<GlobalSettings> {
    return this.http.get<GlobalSettings>(`${this.apiBaseUrl}/settings`);
  }

  saveSettings(settings: GlobalSettings): Observable<GlobalSettings> {
    return this.http.put<GlobalSettings>(`${this.apiBaseUrl}/settings`, settings);
  }

  getLogs(lines = 200): Observable<LogsResponse> {
    const params = new HttpParams().set('lines', lines);
    return this.http.get<LogsResponse>(`${this.apiBaseUrl}/logs`, { params });
  }

  scanNetwork(request: NetworkScanRequest): Observable<NetworkScanResult[]> {
    return this.http.post<NetworkScanResult[]>(`${this.apiBaseUrl}/network/scan`, request);
  }

  createBackup(): Observable<BackupResponse> {
    return this.http.post<BackupResponse>(`${this.apiBaseUrl}/backups`, {});
  }

  restoreBackup(archivePath: string): Observable<{ message: string }> {
    const params = new HttpParams().set('archivePath', archivePath);
    return this.http.post<{ message: string }>(`${this.apiBaseUrl}/backups/restore`, {}, { params });
  }
}
