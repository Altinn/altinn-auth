# Altinn Authorization Metrics

A metric consists of the following properties:

<dl>
  <dt>Meter</dt>
  <dd>A grouping of metrics. This is used to be able to segment out some metrics that should be published to different sources.</dd>
  <dt>Name</dt>
  <dd>The metric name.</dd>
  <dt>Description</dt>
  <dd>A textual description of the metric.</dd>
  <dt>Type</dt>
  <dd>
    A metric type. Can be one of:
    <ul>
      <li>counter</li>
      <li>gauge</li>
      <li>histogram</li>
    </ul>
  </dd>
  <dt>Tags</dt>
  <dd>A set of tags a given meter has. A tag is used to be able to partition points on a metric. All tags <strong>should</strong> be contained in the <a href="#tags">Tags</a> table below.</dd>
  <dt>Long-term storage</dt>
  <dd>Whether or not the metric is exported to long-term storage (and not just application insights).</dd>
</dl>

## Tags

<table>
  <thead>
    <tr>
      <th>Name</th>
      <th>Description</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td><code>resource.owner.org</code></td>
      <td>The organization code of the (service)-owner of the resource.</td>
    </tr>
    <tr>
      <td><code>resource.id</code></td>
      <td>The ID of the (resource-registry) resource.</td>
    </tr>
    <tr>
      <td><code>pdp.api.kind</code> (rename?)</td>
      <td>TODO</td>
    </tr>
  </tbody>
</table>

## Applications

Below is a list of applications that publishes metrics, and what those metrics are called/contain.

### Altinn Authorization

<table>
  <thead>
    <tr>
      <th>Meter</th>
      <th>Name</th>
      <th>Description</th>
      <th>Type</th>
      <th>Tags</th>
      <th>Long-term storage</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>TODO</td>
      <td><code>altinn.pdp.decisions</code> (rename?)</td>
      <td>The number of pdp decisions made.</td>
      <td>counter</td>
      <td>
        <ul>
          <li><code>resource.owner.org</code></li>
          <li><code>resource.id</code></li>
          <li><code>pdp.api.kind</code></li>
        </ul>
      </td>
      <td>❌</td>
    </tr>
  </tbody>
</table>


### Altinn Register

<table>
  <thead>
    <tr>
      <th>Meter</th>
      <th>Name</th>
      <th>Description</th>
      <th>Type</th>
      <th>Tags</th>
      <th>Long-term storage</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>TODO</td>
      <td><code>altinn.register.ccr.online-updates</code></td>
      <td>The number of online updates from CCR.</td>
      <td>counter</td>
      <td></td>
      <td>❌</td>
    </tr>
  </tbody>
</table>
