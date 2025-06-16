<script>
    document.addEventListener('DOMContentLoaded', function() {
        // Load dashboard statistics
        fetch('/Home/GetDashboardStats')
            .then(response => response.json())
            .then(data => {
                document.getElementById('newsCount').textContent = data.News || 0;
                document.getElementById('collectionsCount').textContent = data.Collections || 0;
                document.getElementById('groupsCount').textContent = data.CollectionGroups || 0;
                document.getElementById('productsCount').textContent = data.Products || 0;
            })
            .catch(error => {
                console.error('Error loading dashboard stats:', error);
            });

    // Load recent activity
    fetch('/AuditLog/GetRecentActivity')
        .then(response => response.json())
        .then(data => {
            const tbody = document.getElementById('recentActivity');
    tbody.innerHTML = '';
            
            if (data && data.length > 0) {
        data.forEach(log => {
            const row = `
                        <tr>
                            <td>${log.username}</td>
                            <td><span class="badge bg-primary">${log.action}</span></td>
                            <td>${log.tableName}</td>
                            <td>${new Date(log.createdDate).toLocaleString('tr-TR')}</td>
                        </tr>
                    `;
            tbody.innerHTML += row;
        });
            } else {
        tbody.innerHTML = '<tr><td colspan="4" class="text-center">No recent activity</td></tr>';
            }
        })
        .catch(error => {
        console.error('Error loading recent activity:', error);
    document.getElementById('recentActivity').innerHTML =
    '<tr><td colspan="4" class="text-center text-danger">Error loading activity</td></tr>';
        });
});
</script>
