#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/statvfs.h>

float get_cpu_usage() {
    FILE *fp;
    unsigned long long user, nice, system, idle, iowait, irq, softirq;
    static unsigned long long prev_total = 0, prev_idle = 0;

    fp = fopen("/proc/stat", "r");
    if (!fp) {
        perror("Failed to open /proc/stat");
        return -1;
    }

    fscanf(fp, "cpu %llu %llu %llu %llu %llu %llu %llu",
           &user, &nice, &system, &idle, &iowait, &irq, &softirq);

    fclose(fp);

    unsigned long long total_time = user + nice + system + idle + iowait + irq + softirq;
    unsigned long long idle_time = idle + iowait;

    unsigned long long total_diff = total_time - prev_total;
    unsigned long long idle_diff = idle_time - prev_idle;

    prev_total = total_time;
    prev_idle = idle_time;

    if (total_diff == 0) return 0;
    return 100.0 * (1.0 - ((float)idle_diff / total_diff));
}

void get_memory_usage(float *used_percent, long *total_memory_mb) {
    FILE *fp = fopen("/proc/meminfo", "r");
    if (!fp) {
        perror("Failed to open /proc/meminfo");
        *used_percent = -1;
        *total_memory_mb = -1;
        return;
    }

    unsigned long total_memory = 0, free_memory = 0, buffers = 0, cached = 0;
    char label[32];
    unsigned long value;

    while (fscanf(fp, "%s %lu kB", label, &value) == 2) {
        if (strcmp(label, "MemTotal:") == 0) total_memory = value;
        else if (strcmp(label, "MemFree:") == 0) free_memory = value;
        else if (strcmp(label, "Buffers:") == 0) buffers = value;
        else if (strcmp(label, "Cached:") == 0) cached = value;
    }

    fclose(fp);

    unsigned long used_memory = total_memory - free_memory - buffers - cached;
    *total_memory_mb = total_memory / 1024;
    *used_percent = (float)used_memory / total_memory * 100.0;
}

void get_disk_io(const char *device, float *read_kbps, float *write_kbps) {
    static unsigned long long prev_read_sectors = 0, prev_write_sectors = 0;
    unsigned long long read_sectors = 0, write_sectors = 0;
    unsigned int sector_size = 512;
    char line[256];
    FILE *fp;

    fp = fopen("/proc/diskstats", "r");
    if (!fp) {
        perror("Failed to open /proc/diskstats");
        *read_kbps = -1;
        *write_kbps = -1;
        return;
    }

    while (fgets(line, sizeof(line), fp)) {
        char dev_name[32];
        unsigned long long reads, writes;

        if (sscanf(line, "%*d %*d %s %*u %*u %llu %*u %*u %*u %llu", dev_name, &reads, &writes) == 3) {
            if (strcmp(dev_name, device) == 0) {
                read_sectors = reads;
                write_sectors = writes;
                break;
            }
        }
    }

    fclose(fp);

    *read_kbps = ((read_sectors - prev_read_sectors) * sector_size) / 1024.0 / 1024.0;
    *write_kbps = ((write_sectors - prev_write_sectors) * sector_size) / 1024.0 / 1024.0;

    prev_read_sectors = read_sectors;
    prev_write_sectors = write_sectors;
}

int main(int argc, char *argv[]) {
    if (argc < 2) {
        fprintf(stderr, "Usage: %s [cpu|memory|disk <device>]\n", argv[0]);
        return EXIT_FAILURE;
    }

    if (strcmp(argv[1], "cpu") == 0) 
    {
        float usage = get_cpu_usage();
        if (usage < 0) {
            fprintf(stderr, "Error reading CPU stats\n");
            return EXIT_FAILURE;
        }
        printf("{\"type\": \"cpu\", \"usage_percent\": %.2f}\n", usage);
    } 
    else if (strcmp(argv[1], "memory") == 0) 
    {
        float used_percent;
        long total_memory_mb;
        get_memory_usage(&used_percent, &total_memory_mb);
        if (used_percent < 0 || total_memory_mb < 0) {
            fprintf(stderr, "Error reading memory stats\n");
            return EXIT_FAILURE;
        }
        printf("{\"type\": \"memory\", \"used_percent\": %.2f, \"total_memory_mb\": %ld}\n", used_percent, total_memory_mb);
    } 
    else if (strcmp(argv[1], "disk") == 0) 
    {
        if (argc < 3) 
        {
            fprintf(stderr, "Usage: %s disk <device>\n", argv[0]);
            return EXIT_FAILURE;
        }
        float read_kbps, write_kbps;
        get_disk_io(argv[2], &read_kbps, &write_kbps);
        if (read_kbps < 0 || write_kbps < 0) 
        {
            fprintf(stderr, "Error reading disk stats\n");
            return EXIT_FAILURE;
        }
        printf("{\"type\": \"disk\", \"device\": \"%s\", \"read_mbps\": %.2f, \"write_mbps\": %.2f}\n", 
                argv[2], read_kbps, write_kbps);
    } 
    else 
    {
        fprintf(stderr, "Unknown command: %s\n", argv[1]);
        return EXIT_FAILURE;
    }

    return EXIT_SUCCESS;
}