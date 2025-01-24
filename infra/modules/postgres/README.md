## Introduction
When configuring max_


## MS Docs
* [Pooling](https://learn.microsoft.com/en-us/azure/postgresql/flexible-server/concepts-limits)
 
## Burstable				
| Product Name | vCores | Memory  size | Maximum connections | Maximum  user connections |
| ------------ | ------ | ------------ | ------------------- | ------------------------- |
| B1ms         | 1      | 2 GiB        | 50                  | 35                        |
| B2s          | 2      | 4 GiB        | 429                 | 414                       |
| B2ms         | 2      | 8 GiB        | 859                 | 844                       |
| B4ms         | 4      | 16 GiB       | 1,718               | 1,703                     |
| B8ms         | 8      | 32 GiB       | 3,437               | 3,422                     |
| B12ms        | 12     | 48 GiB       | 5,000               | 4,985                     |
| B16ms        | 16     | 64 GiB       | 5,000               | 4,985                     |
| B20ms        | 20     | 80 GiB       | 5,000               | 4,985                     |

## General Purpose				
| Product Name                              | vCores | Memory  size | Maximum connections | Maximum  user connections |
| ----------------------------------------- | ------ | ------------ | ------------------- | ------------------------- |
| D2s_v3 / D2ds_v4 / D2ds_v5 / D2ads_v5     | 2      | 8 GiB        | 859                 | 844                       |
| D4s_v3 / D4ds_v4 / D4ds_v5 / D4ads_v5     | 4      | 16 GiB       | 1,718               | 1,703                     |
| D8s_v3 / D8ds_V4 / D8ds_v5 / D8ads_v5     | 8      | 32 GiB       | 3,437               | 3,422                     |
| D16s_v3 / D16ds_v4 / D16ds_v5 / D16ads_v5 | 16     | 64 GiB       | 5,000               | 4,985                     |
| D32s_v3 / D32ds_v4 / D32ds_v5 / D32ads_v5 | 32     | 128 GiB      | 5,000               | 4,985                     |
| D48s_v3 / D48ds_v4 / D48ds_v5 / D48ads_v5 | 48     | 192 GiB      | 5,000               | 4,985                     |
| D64s_v3 / D64ds_v4 / D64ds_v5 / D64ads_v5 | 64     | 256 GiB      | 5,000               | 4,985                     |
| D96ds_v5 / D96ads_v5                      | 96     | 384 GiB      | 5,000               | 4,985                     |

## Memory Optimized				
| Product Name                              | vCores | Memory  size | Maximum connections | Maximum  user connections |
| ----------------------------------------- | ------ | ------------ | ------------------- | ------------------------- |
| E2s_v3 / E2ds_v4 / E2ds_v5 / E2ads_v5     | 2      | 16 GiB       | 1,718               | 1,703                     |
| E4s_v3 / E4ds_v4 / E4ds_v5 / E4ads_v5     | 4      | 32 GiB       | 3,437               | 3,422                     |
| E8s_v3 / E8ds_v4 / E8ds_v5 / E8ads_v5     | 8      | 64 GiB       | 5,000               | 4,985                     |
| E16s_v3 / E16ds_v4 / E16ds_v5 / E16ads_v5 | 16     | 128 GiB      | 5,000               | 4,985                     |
| E20ds_v4 / E20ds_v5 / E20ads_v5           | 20     | 160 GiB      | 5,000               | 4,985                     |
| E32s_v3 / E32ds_v4 / E32ds_v5 / E32ads_v5 | 32     | 256 GiB      | 5,000               | 4,985                     |
| E48s_v3 / E48ds_v4 / E48ds_v5 / E48ads_v5 | 48     | 384 GiB      | 5,000               | 4,985                     |
| E64s_v3 / E64ds_v4 / E64ds_v5 / E64ads_v5 | 64     | 432 GiB      | 5,000               | 4,985                     |
| E96ds_v5 / E96ads_v5                      | 96     | 672 GiB      | 5,000               | 4,985                     |

## Notes
* Although it's possible to increase the value of max_connections beyond the default setting, we advise against it.
