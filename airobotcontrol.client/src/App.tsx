import { useEffect, useState } from 'react';
import { HashRouter as Router, Routes, Route, Link } from 'react-router-dom';
import RobotsDemo from './demos/robots/RobotsDemo';
import './App.css';

interface Forecast {
    date: string;
    temperatureC: number;
    temperatureF: number;
    summary: string;
}

function HomePage() {
    const [forecasts, setForecasts] = useState<Forecast[]>();

    useEffect(() => {
        populateWeatherData();
    }, []);

    const contents = forecasts === undefined
        ? <p><em>Loading... Please refresh once the ASP.NET backend has started. See <a href="https://aka.ms/jspsintegrationreact">https://aka.ms/jspsintegrationreact</a> for more details.</em></p>
        : <table className="table table-striped" aria-labelledby="tableLabel">
            <thead>
                <tr>
                    <th>Date</th>
                    <th>Temp. (C)</th>
                    <th>Temp. (F)</th>
                    <th>Summary</th>
                </tr>
            </thead>
            <tbody>
                {forecasts.map(forecast =>
                    <tr key={forecast.date}>
                        <td>{forecast.date}</td>
                        <td>{forecast.temperatureC}</td>
                        <td>{forecast.temperatureF}</td>
                        <td>{forecast.summary}</td>
                    </tr>
                )}
            </tbody>
        </table>;

    return (
        <div>
            <h1 id="tableLabel">AI Robot Control</h1>
            <p>Welcome to the AI Robot Control application.</p>
            
            <h2>Available Demos</h2>
            <ul>
                <li>
                    <Link to="/robots-demo">Robots Demo (React)</Link> - Refactored TypeScript version
                </li>
                <li>
                    <a href="/HtmlDemos/Robots.html" target="_blank" rel="noopener noreferrer">
                        Robots Demo (Original HTML)
                    </a> - Original HTML version
                </li>
            </ul>
            <p style={{ marginTop: '10px', fontSize: '12px', opacity: 0.7 }}>
                Note: For React demo, navigate to <code>/#/robots-demo</code> in the URL
            </p>

            <h2>Weather Forecast</h2>
            <p>This component demonstrates fetching data from the server.</p>
            {contents}
        </div>
    );

    async function populateWeatherData() {
        const response = await fetch('weatherforecast');
        if (response.ok) {
            const data = await response.json();
            setForecasts(data);
        }
    }
}

function App() {
    return (
        <Router>
            <Routes>
                <Route path="/" element={<HomePage />} />
                <Route path="/robots-demo" element={<RobotsDemo />} />
            </Routes>
        </Router>
    );
}

export default App;