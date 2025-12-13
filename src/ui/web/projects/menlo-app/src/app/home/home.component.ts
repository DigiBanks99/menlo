import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  template: `
    <div class="home-container">
      <header>
        <h1>Menlo Home Management</h1>
        <p>Your intelligent family budget management system</p>
      </header>

      <nav class="main-nav">
        <a routerLink="/budgets" class="nav-button">View Budgets</a>
        <a routerLink="/analytics" class="nav-button">Analytics</a>
      </nav>

      <!-- Deferrable view for analytics preview - loads only when visible -->
      @defer (on viewport) {
        <div class="preview-section">
          <h2>Quick Overview</h2>
          <div class="preview-cards">
            <div class="preview-card">
              <h3>Total Budget</h3>
              <p class="amount">R 1,000</p>
            </div>
            <div class="preview-card">
              <h3>Spent This Month</h3>
              <p class="amount">R 650</p>
            </div>
            <div class="preview-card">
              <h3>Remaining</h3>
              <p class="amount">R 350</p>
            </div>
          </div>
        </div>
      } @placeholder {
        <div class="loading-placeholder">
          <p>Loading overview...</p>
        </div>
      } @error {
        <div class="error-placeholder">
          <p>Failed to load overview</p>
        </div>
      }
    </div>
  `,
  styles: [`
    .home-container {
      padding: 2rem;
      max-width: 1000px;
      margin: 0 auto;
    }

    header {
      text-align: center;
      margin-bottom: 3rem;
    }

    header h1 {
      color: #2c3e50;
      margin-bottom: 0.5rem;
    }

    header p {
      color: #6c757d;
      font-size: 1.1rem;
    }

    .main-nav {
      display: flex;
      justify-content: center;
      gap: 1rem;
      margin-bottom: 3rem;
    }

    .nav-button {
      display: inline-block;
      padding: 1rem 2rem;
      background: #007bff;
      color: white;
      text-decoration: none;
      border-radius: 8px;
      font-weight: 500;
      transition: background-color 0.2s;
    }

    .nav-button:hover {
      background: #0056b3;
    }

    .preview-section {
      margin-top: 3rem;
      padding: 2rem;
      border: 1px solid #e9ecef;
      border-radius: 12px;
      background: #f8f9fa;
    }

    .preview-cards {
      display: grid;
      gap: 1rem;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      margin-top: 1rem;
    }

    .preview-card {
      padding: 1.5rem;
      background: white;
      border-radius: 8px;
      text-align: center;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .preview-card h3 {
      margin: 0 0 0.5rem 0;
      color: #495057;
      font-size: 0.9rem;
    }

    .amount {
      font-size: 1.8rem;
      font-weight: bold;
      color: #28a745;
      margin: 0;
    }

    .loading-placeholder {
      margin-top: 3rem;
      padding: 2rem;
      text-align: center;
      color: #6c757d;
      background: #f8f9fa;
      border-radius: 12px;
      border: 1px solid #e9ecef;
    }

    .error-placeholder {
      margin-top: 3rem;
      padding: 2rem;
      text-align: center;
      color: #dc3545;
      background: #f8d7da;
      border-radius: 12px;
      border: 1px solid #f5c6cb;
    }
  `]
})
export class HomeComponent {}
